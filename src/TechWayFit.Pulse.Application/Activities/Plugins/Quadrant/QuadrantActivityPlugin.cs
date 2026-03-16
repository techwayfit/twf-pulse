using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Quadrant;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.Quadrant"/> activity type.
/// </summary>
public sealed class QuadrantActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.Quadrant;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "Quadrant",
        FaIconClass: "fa-solid fa-border-all",
        BadgeColorHex: "#6f42c1",
        BadgeTextColorHex: "#ffffff",
        AcceptsResponses: true,
        ConfigType: typeof(QuadrantConfig),
        ResponsePayloadType: typeof(QuadrantItemResponse));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"xAxisLabel":"Complexity","yAxisLabel":"Effort","xScoreOptions":[{"value":"1","label":"1"},{"value":"2","label":"2"},{"value":"3","label":"3"},{"value":"4","label":"4"},{"value":"5","label":"5"}],"yScoreOptions":[],"items":["Item 1","Item 2"],"bubbleSizeMode":"Proportional"}""";

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        if (string.IsNullOrWhiteSpace(config))
            return GetDefaultConfig();

        try
        {
            using var doc = JsonDocument.Parse(config);
            return config;
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
            list.Add("Quadrant config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (!root.TryGetProperty("items", out var itemsElem)
                || itemsElem.ValueKind != JsonValueKind.Array
                || itemsElem.GetArrayLength() < 1)
            {
                list.Add("Quadrant config must contain at least 1 item.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"Quadrant config is not valid JSON: {ex.Message}");
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

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("itemIndex", out _)
                && root.TryGetProperty("xValue", out _)
                && root.TryGetProperty("yValue", out _))
            {
                error = null;
                return true;
            }

            error = "Quadrant response payload must be an object with 'itemIndex', 'xValue', and 'yValue'.";
            return false;
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

        var activity = await dataContext.GetActivityAsync(activityId, cancellationToken)
            ?? throw new ArgumentException("Activity not found.", nameof(activityId));

        var config      = ParseConfig(activity.Config);
        var allResponses = await dataContext.GetResponsesAsync(activityId, cancellationToken);
        var filtered    = ApplyFilters(allResponses, filters);
        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);

        var respondedParticipants = filtered.Select(r => r.ParticipantId).Distinct().Count();
        var lastResponseAt        = filtered.Count == 0 ? (DateTimeOffset?)null : filtered.Max(r => r.CreatedAt);
        var items                 = BuildItemAggregates(filtered, config);

        var result = new QuadrantDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            config?.XAxisLabel ?? "X",
            config?.YAxisLabel ?? "Y",
            filtered.Count,
            participants.Count,
            respondedParticipants,
            items,
            lastResponseAt);

        return new QuadrantDashboardData(result);
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

    private static QuadrantConfig? ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<QuadrantConfig>(
                configJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<QuadrantItemAggregate> BuildItemAggregates(
        IReadOnlyList<Domain.Entities.Response> responses,
        QuadrantConfig? config)
    {
        var groups = new Dictionary<int, List<(double x, double y, string? note, DateTimeOffset createdAt)>>();

        foreach (var response in responses)
        {
            if (!TryParseItemResponse(response.Payload, out var itemIndex, out var x, out var y, out var note))
                continue;

            if (!groups.TryGetValue(itemIndex, out var list))
            {
                list = new List<(double, double, string?, DateTimeOffset)>();
                groups[itemIndex] = list;
            }

            list.Add((x, y, note, response.CreatedAt));
        }

        var aggregates  = new List<QuadrantItemAggregate>();
        var itemLabels  = config?.Items ?? new List<string>();

        foreach (var (idx, scores) in groups.OrderBy(g => g.Key))
        {
            var label = idx >= 0 && idx < itemLabels.Count
                ? itemLabels[idx]
                : $"Item {idx + 1}";

            var avgX  = scores.Average(s => s.x);
            var avgY  = scores.Average(s => s.y);
            var notes = scores
                .Where(s => !string.IsNullOrWhiteSpace(s.note))
                .Select(s => new QuadrantItemNote(s.note!, s.createdAt))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            aggregates.Add(new QuadrantItemAggregate(idx, label, avgX, avgY, scores.Count, notes));
        }

        return aggregates;
    }

    private static bool TryParseItemResponse(
        string payload,
        out int itemIndex,
        out double x,
        out double y,
        out string? note)
    {
        itemIndex = 0;
        x         = 0;
        y         = 0;
        note      = null;

        if (string.IsNullOrWhiteSpace(payload)) return false;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return false;

            if (!root.TryGetProperty("itemIndex", out var idxEl) || !idxEl.TryGetInt32(out itemIndex))
                return false;

            if (!root.TryGetProperty("xValue", out var xEl)) return false;
            if (!root.TryGetProperty("yValue", out var yEl)) return false;

            var xStr = xEl.ValueKind == JsonValueKind.String ? xEl.GetString() : xEl.GetRawText();
            var yStr = yEl.ValueKind == JsonValueKind.String ? yEl.GetString() : yEl.GetRawText();

            if (!double.TryParse(xStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out x)) return false;
            if (!double.TryParse(yStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out y)) return false;

            if (root.TryGetProperty("note", out var noteEl) && noteEl.ValueKind == JsonValueKind.String)
                note = noteEl.GetString();

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
