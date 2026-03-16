using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Poll;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.Poll"/> activity type.
/// <para>
/// Encapsulates all poll-specific rules: default config, config limit enforcement,
/// config validation, response validation, AI participation, and live dashboard data.
/// </para>
/// </summary>
public sealed class PollActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.Poll;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "Poll",
        FaIconClass: "fa-solid fa-chart-bar",
        BadgeColorHex: "#0d6efd",
        BadgeTextColorHex: "#ffffff",
        AcceptsResponses: true,
        ConfigType: typeof(PollConfig),
        ResponsePayloadType: typeof(PollResponse));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"options":[{"id":"option_0","label":"Option A"},{"id":"option_1","label":"Option B"}],"maxResponsesPerParticipant":1,"allowMultipleSelections":false}""";

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        if (string.IsNullOrWhiteSpace(config))
        {
            return GetDefaultConfig();
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            var max = defaults.PollMaxResponsesPerParticipant;

            // Only rewrite when the stored value already exceeds the server limit.
            if (root.TryGetProperty("maxResponsesPerParticipant", out var maxElem)
                && maxElem.TryGetInt32(out var stored)
                && stored <= max)
            {
                return config;
            }

            // Rebuild JSON with the clamped value.
            using var ms = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "maxResponsesPerParticipant")
                    writer.WriteNumber("maxResponsesPerParticipant", max);
                else
                    prop.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        catch (JsonException)
        {
            return GetDefaultConfig();
        }
    }

    public bool ValidateConfig(string? config, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();

        if (string.IsNullOrWhiteSpace(config))
        {
            list.Add("Poll config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (!root.TryGetProperty("options", out var optionsElem)
                || optionsElem.ValueKind != JsonValueKind.Array)
            {
                list.Add("Poll config must contain an 'options' array.");
            }
            else if (optionsElem.GetArrayLength() < 2)
            {
                list.Add("A poll must have at least 2 options.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"Poll config is not valid JSON: {ex.Message}");
        }

        errors = list;
        return list.Count == 0;
    }

    // ── Response contract ─────────────────────────────────────────────────────

    public bool AcceptsResponses => Metadata.AcceptsResponses;

    public bool ValidateResponsePayload(string payload, out string? error)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "Response payload is required.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Accept: array, object with selectedOptionIds, object with selectedOptions
            switch (root.ValueKind)
            {
                case JsonValueKind.Array:
                case JsonValueKind.String:
                    error = null;
                    return true;
                case JsonValueKind.Object
                    when root.TryGetProperty("selectedOptionIds", out _)
                         || root.TryGetProperty("selectedOptions", out _):
                    error = null;
                    return true;
                default:
                    error = "Poll response payload must be an array of selected option IDs or an object with 'selectedOptionIds'.";
                    return false;
            }
        }
        catch (JsonException ex)
        {
            error = $"Response payload is not valid JSON: {ex.Message}";
            return false;
        }
    }

    // ── AI participation ──────────────────────────────────────────────────────

    public bool IncludeInAiSummary => true;

    public bool CanBeAiGenerated => true;

    // ── Dashboard data ────────────────────────────────────────────────────────

    public async Task<IActivityDashboardData> GetDashboardDataAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        IActivityDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session id is required.", nameof(sessionId));

        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity id is required.", nameof(activityId));

        var activity = await dataContext.GetActivityAsync(activityId, cancellationToken);
        if (activity is null)
            throw new ArgumentException("Activity not found.", nameof(activityId));

        var allResponses = await dataContext.GetResponsesAsync(activityId, cancellationToken);
        var filteredResponses = ApplyFilters(allResponses, filters);

        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);
        var respondedParticipants = filteredResponses.Select(r => r.ParticipantId).Distinct().Count();
        var lastResponseAt = filteredResponses.Count == 0
            ? (DateTimeOffset?)null
            : filteredResponses.Max(r => r.CreatedAt);

        var pollConfig = ParseConfig(activity.Config ?? "{}");
        var optionCounts = CountVotes(filteredResponses, pollConfig);
        var totalVotes = optionCounts.Values.Sum();

        var results = pollConfig.Options
            .Select(opt =>
            {
                var count = optionCounts.TryGetValue(opt, out var c) ? c : 0;
                var pct = totalVotes > 0 ? count / (double)totalVotes * 100.0 : 0.0;
                return new PollOptionResult(opt.Id, opt.Label, count, pct);
            })
            .ToList();

        var response = new PollDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            filteredResponses.Count,
            participants.Count,
            respondedParticipants,
            results,
            lastResponseAt);

        return new PollDashboardData(response);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static List<Domain.Entities.Response> ApplyFilters(
        IReadOnlyList<Domain.Entities.Response> responses,
        IReadOnlyDictionary<string, string?> filters)
    {
        if (filters is null || filters.Count == 0)
            return responses.ToList();

        return responses
            .Where(r => MatchesFilters(r.Dimensions, filters))
            .ToList();
    }

    private static bool MatchesFilters(
        IReadOnlyDictionary<string, string?> dimensions,
        IReadOnlyDictionary<string, string?> filters)
    {
        foreach (var filter in filters)
        {
            if (string.IsNullOrEmpty(filter.Value))
                continue;

            if (!dimensions.TryGetValue(filter.Key, out var value)
                || !string.Equals(value, filter.Value, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static PollConfiguration ParseConfig(string config)
    {
        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;
            var options = new List<PollOption>();

            if (root.TryGetProperty("options", out var optionsElem)
                && optionsElem.ValueKind == JsonValueKind.Array)
            {
                var idx = 0;
                foreach (var elem in optionsElem.EnumerateArray())
                {
                    if (elem.ValueKind == JsonValueKind.String)
                    {
                        var label = elem.GetString();
                        if (!string.IsNullOrEmpty(label))
                            options.Add(new PollOption($"option_{idx}", label));
                    }
                    else if (elem.ValueKind == JsonValueKind.Object)
                    {
                        var label = elem.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(label))
                        {
                            var id = elem.TryGetProperty("id", out var idElem)
                                ? idElem.GetString() ?? $"option_{idx}"
                                : $"option_{idx}";
                            options.Add(new PollOption(id, label));
                        }
                    }
                    idx++;
                }
            }

            return new PollConfiguration(options);
        }
        catch (JsonException)
        {
            return new PollConfiguration(new List<PollOption>());
        }
    }

    private static Dictionary<PollOption, int> CountVotes(
        IReadOnlyList<Domain.Entities.Response> responses,
        PollConfiguration config)
    {
        var counts = config.Options.ToDictionary(opt => opt, _ => 0);

        foreach (var response in responses)
        {
            foreach (var id in ParseResponsePayload(response.Payload))
            {
                var opt = config.Options.FirstOrDefault(o => o.Id == id);
                if (opt is not null)
                    counts[opt]++;
            }
        }

        return counts;
    }

    private static IReadOnlyList<string> ParseResponsePayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
                return ExtractStrings(root);

            if (root.TryGetProperty("selectedOptionIds", out var ids)
                && ids.ValueKind == JsonValueKind.Array)
                return ExtractStrings(ids);

            if (root.TryGetProperty("selectedOptions", out var opts)
                && opts.ValueKind == JsonValueKind.Array)
                return ExtractStrings(opts);

            if (root.ValueKind == JsonValueKind.String)
            {
                var s = root.GetString();
                return string.IsNullOrEmpty(s) ? [] : [s];
            }

            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ExtractStrings(JsonElement array)
    {
        var list = new List<string>();
        foreach (var elem in array.EnumerateArray())
        {
            if (elem.ValueKind == JsonValueKind.String)
            {
                var s = elem.GetString();
                if (!string.IsNullOrEmpty(s))
                    list.Add(s);
            }
        }
        return list;
    }

    private sealed record PollConfiguration(IReadOnlyList<PollOption> Options);
    private sealed record PollOption(string Id, string Label);
}
