using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Activities.Abstractions;

/// <summary>
/// The single contract that every activity type must implement.
/// <para>
/// Implementing this interface is the ONE place all activity-specific knowledge lives:
/// its config shape, response validation, dashboard data computation, and AI participation rules.
/// </para>
/// <para>
/// The corresponding UI component types (Blazor Participant view, Dashboard, Presentation)
/// are registered separately via <c>IActivityUiDescriptor</c> in the Web layer, keeping the
/// Application layer free of UI dependencies.
/// </para>
/// </summary>
public interface IActivityPlugin
{
    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>The enum value this plugin owns. Must be unique across all registered plugins.</summary>
    ActivityType ActivityType { get; }

    /// <summary>Display and rendering metadata (icon, badge colour, display name).</summary>
    ActivityPluginMetadata Metadata { get; }

    // ── Config contract ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a valid JSON config string with sensible defaults for this activity type.
    /// Called when AI generation omits config, or when a facilitator adds via the default form.
    /// </summary>
    string GetDefaultConfig();

    /// <summary>
    /// Applies system-enforced limits to an AI-generated or user-provided config string.
    /// Returns the (possibly modified) JSON config.
    /// </summary>
    string EnforceConfigLimits(string? config, IActivityDefaults defaults);

    /// <summary>
    /// Validates a config JSON string for this activity type.
    /// Returns true when valid; populates <paramref name="errors"/> on failure.
    /// </summary>
    bool ValidateConfig(string? config, out IReadOnlyList<string> errors);

    // ── Response contract ────────────────────────────────────────────────────

    /// <summary>True when participants submit a response payload (e.g. Poll, WordCloud).</summary>
    bool AcceptsResponses { get; }

    /// <summary>
    /// Validates a participant's response payload JSON string.
    /// Called by ResponseService before persisting.
    /// Only invoked when <see cref="AcceptsResponses"/> is true.
    /// Returns true when valid; sets <paramref name="error"/> on failure.
    /// </summary>
    bool ValidateResponsePayload(string payload, out string? error);

    // ── AI participation ────────────────────────────────────────────────────

    /// <summary>
    /// When true, responses from this activity type are included when building an AI summary.
    /// Set to false for Break and AiSummary activities.
    /// </summary>
    bool IncludeInAiSummary { get; }

    /// <summary>
    /// When true, the AI session generator can suggest this activity type.
    /// Set to false for AiSummary (it is added explicitly, not generated).
    /// </summary>
    bool CanBeAiGenerated { get; }

    // ── Dashboard data ──────────────────────────────────────────────────────

    /// <summary>
    /// Computes and returns live dashboard data for the facilitator view.
    /// The returned <see cref="IActivityDashboardData"/> is the activity's own
    /// concrete data type (e.g. <c>PollDashboardData</c>). The Web layer casts
    /// it using the known plugin type after calling <see cref="IActivityRegistry.GetPlugin"/>.
    /// </summary>
    Task<IActivityDashboardData> GetDashboardDataAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        IActivityDataContext dataContext,
        CancellationToken cancellationToken = default);
}
