using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Break;

/// <summary>
/// Maps the Break activity type to its Blazor components:
/// participant view, facilitator dashboard, and presentation view.
/// </summary>
public sealed class BreakActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.Break;
    public Type? ParticipantComponentType  => typeof(BreakActivity);
    public Type? DashboardComponentType    => typeof(BreakDashboard);
    public Type? PresentationComponentType => typeof(BreakPresentation);
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;

    // BreakPresentation only accepts SessionCode, ActivityId, ActivityTitle, ActivityConfig.
    // It does NOT have ActivityPrompt, DurationMinutes, or OpenedAt parameters.
    public IDictionary<string, object?> BuildPresentationParameters(
        string sessionCode,
        Guid activityId,
        string? activityTitle,
        string? activityPrompt,
        int? durationMinutes,
        DateTimeOffset? openedAt,
        string? activityConfig)
        => new Dictionary<string, object?>
        {
            { "SessionCode",    sessionCode },
            { "ActivityId",     activityId },
            { "ActivityTitle",  activityTitle },
            { "ActivityConfig", activityConfig },
        };
}
