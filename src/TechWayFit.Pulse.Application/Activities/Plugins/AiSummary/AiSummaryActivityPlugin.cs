using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;

namespace TechWayFit.Pulse.Application.Activities.Plugins.AiSummary;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.AiSummary"/> activity type.
/// AiSummary does not accept participant responses; it presents an AI-generated summary.
/// </summary>
public sealed class AiSummaryActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.AiSummary;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "AI Summary",
        FaIconClass: "fa-solid fa-robot",
        BadgeColorHex: "#6c757d",
        BadgeTextColorHex: "#ffffff",
        AcceptsResponses: false,
        ConfigType: typeof(AiSummaryConfig),
        ResponsePayloadType: null);

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"customPromptAddition":"","showActivityBreakdown":true,"generatedSummary":"","isGenerating":false,"generatedAt":null}""";

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
            list.Add("AiSummary config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
        }
        catch (JsonException ex)
        {
            list.Add($"AiSummary config is not valid JSON: {ex.Message}");
        }

        errors = list;
        return list.Count == 0;
    }

    // ── Response contract ─────────────────────────────────────────────────────

    public bool AcceptsResponses => Metadata.AcceptsResponses;

    public bool ValidateResponsePayload(string payload, out string? error)
    {
        error = "AiSummary does not accept responses.";
        return false;
    }

    // ── AI participation ──────────────────────────────────────────────────────

    public bool IncludeInAiSummary => false;

    public bool CanBeAiGenerated => false;

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

        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);

        return new AiSummaryDashboardData(
            sessionId,
            activityId,
            activity.Title,
            participants.Count);
    }
}
