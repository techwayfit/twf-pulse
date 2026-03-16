using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Web.Activities;

/// <summary>
/// Maps an activity type to its Blazor component types.
/// Kept in the Web layer (not Application) so the Application layer
/// has zero dependency on Microsoft.AspNetCore.Components.
/// <para>
/// Each activity registers its own implementation. The four component types
/// replace the four <c>if/else ActivityType == ...</c> dispatch chains in:
/// <list type="bullet">
///   <item><c>Pages/Participant/Activity.razor</c></item>
///   <item><c>Components/Facilitator/LiveCurrentActivity.razor</c></item>
///   <item><c>Pages/Facilitator/Presentation.razor</c></item>
///   <item><c>Components/Facilitator/EditActivityModal.razor</c></item>
/// </list>
/// </para>
/// </summary>
public interface IActivityUiDescriptor
{
    ActivityType ActivityType { get; }

    /// <summary>
    /// Blazor component shown to the participant during a live activity
    /// (e.g. <c>typeof(PollActivity)</c>). Null if the activity has no participant view.
    /// </summary>
    Type? ParticipantComponentType { get; }

    /// <summary>
    /// Blazor component shown in the facilitator's live dashboard
    /// (e.g. <c>typeof(PollDashboard)</c>). Null if the activity has no live dashboard.
    /// </summary>
    Type? DashboardComponentType { get; }

    /// <summary>
    /// Blazor component shown in the facilitator's full-screen presentation mode
    /// (e.g. <c>typeof(PollPresentation)</c>). Null if not supported.
    /// </summary>
    Type? PresentationComponentType { get; }

    /// <summary>
    /// Blazor component rendered inside <c>EditActivityModal</c> for editing
    /// activity-specific config fields (e.g. <c>typeof(PollEditConfig)</c>).
    /// Null if the activity has no editable config.
    /// </summary>
    Type? EditConfigComponentType { get; }

    /// <summary>
    /// Blazor component rendered as the "Add [type]" modal form
    /// (e.g. <c>typeof(PollCreateModal)</c>). Null if not yet implemented.
    /// </summary>
    Type? CreateModalComponentType { get; }

    /// <summary>
    /// Builds the parameter dictionary passed to <see cref="DashboardComponentType"/> via
    /// <c>DynamicComponent</c>. Override when the dashboard component needs parameters beyond
    /// the standard <c>SessionCode</c> / <c>ActivityId</c> pair
    /// (e.g. Quadrant also passes <c>ActivityConfig</c>).
    /// </summary>
    IDictionary<string, object?> BuildDashboardParameters(string sessionCode, Guid activityId, string? activityConfig)
        => new Dictionary<string, object?> { { "SessionCode", sessionCode }, { "ActivityId", activityId } };

    /// <summary>
    /// Builds the parameter dictionary passed to <see cref="PresentationComponentType"/> via
    /// <c>DynamicComponent</c>. Override when the presentation component needs parameters beyond
    /// the standard set (<c>SessionCode</c>, <c>ActivityId</c>, <c>ActivityTitle</c>,
    /// <c>ActivityPrompt</c>, <c>DurationMinutes</c>, <c>OpenedAt</c>).
    /// </summary>
    IDictionary<string, object?> BuildPresentationParameters(
        string sessionCode,
        Guid activityId,
        string? activityTitle,
        string? activityPrompt,
        int? durationMinutes,
        DateTimeOffset? openedAt,
        string? activityConfig)
        => new Dictionary<string, object?>
        {
            { "SessionCode",      sessionCode },
            { "ActivityId",       activityId },
            { "ActivityTitle",    activityTitle },
            { "ActivityPrompt",   activityPrompt },
            { "DurationMinutes",  durationMinutes },
            { "OpenedAt",         openedAt },
        };
}
