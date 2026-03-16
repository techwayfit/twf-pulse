using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Break;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.Break"/> activity type.
/// Break does not accept traditional participant responses.
/// </summary>
public sealed class BreakActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.Break;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "Break",
        FaIconClass: "fa-solid fa-mug-hot",
        BadgeColorHex: "#adb5bd",
        BadgeTextColorHex: "#000000",
        AcceptsResponses: false,
        ConfigType: typeof(BreakConfig),
        ResponsePayloadType: typeof(BreakResponse));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"message":"Take a short break. We'll resume shortly!","durationMinutes":15,"showCountdown":true,"allowReadySignal":true}""";

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
            list.Add("Break config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (root.TryGetProperty("durationMinutes", out var durationElem)
                && durationElem.TryGetInt32(out var duration)
                && duration < 1)
            {
                list.Add("durationMinutes must be at least 1.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"Break config is not valid JSON: {ex.Message}");
        }

        errors = list;
        return list.Count == 0;
    }

    // ── Response contract ─────────────────────────────────────────────────────

    public bool AcceptsResponses => Metadata.AcceptsResponses;

    public bool ValidateResponsePayload(string payload, out string? error)
    {
        error = "Break does not accept responses.";
        return false;
    }

    // ── AI participation ──────────────────────────────────────────────────────

    public bool IncludeInAiSummary => false;

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

        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);

        return new BreakDashboardData(
            sessionId,
            activityId,
            activity.Title,
            participants.Count);
    }
}
