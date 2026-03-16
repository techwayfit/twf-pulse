using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.FiveWhys;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.FiveWhys"/> activity type.
/// </summary>
public sealed class FiveWhysActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.FiveWhys;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "5 Whys",
        FaIconClass: "fa-solid fa-sitemap",
        BadgeColorHex: "#fd7e14",
        BadgeTextColorHex: "#ffffff",
        AcceptsResponses: true,
        ConfigType: typeof(FiveWhysConfig),
        ResponsePayloadType: typeof(FiveWhysResponsePayload));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"rootQuestion":"Why is this problem occurring?","context":null,"maxDepth":5}""";

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
            list.Add("FiveWhys config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (!root.TryGetProperty("rootQuestion", out var rq)
                || rq.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(rq.GetString()))
            {
                list.Add("FiveWhys config must contain a non-empty 'rootQuestion'.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"FiveWhys config is not valid JSON: {ex.Message}");
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

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("chain", out _))
            {
                error = null;
                return true;
            }

            error = "FiveWhys response payload must be an object with a 'chain' array.";
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

        var activity = await dataContext.GetActivityAsync(activityId, cancellationToken);
        if (activity is null)
            throw new ArgumentException("Activity not found.", nameof(activityId));

        var allResponses = await dataContext.GetResponsesAsync(activityId, cancellationToken);
        var filtered     = ApplyFilters(allResponses, filters);
        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);

        var respondedParticipants = filtered.Select(r => r.ParticipantId).Distinct().Count();
        var lastResponseAt        = filtered.Count == 0 ? (DateTimeOffset?)null : filtered.Max(r => r.CreatedAt);

        var summaries = filtered
            .Select(r => new FiveWhysResponseSummary(r.Id, r.ParticipantId, r.Payload, r.CreatedAt))
            .ToList();

        return new FiveWhysDashboardData(
            sessionId,
            activityId,
            activity.Title,
            filtered.Count,
            participants.Count,
            respondedParticipants,
            lastResponseAt,
            summaries);
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
}
