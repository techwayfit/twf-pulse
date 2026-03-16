using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Quadrant;

/// <summary>
/// Maps the Quadrant activity type to its three Blazor components:
/// participant input, facilitator dashboard, and presentation view.
/// </summary>
public sealed class QuadrantActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.Quadrant;
    public Type? ParticipantComponentType  => typeof(QuadrantActivity);
    public Type? DashboardComponentType    => typeof(QuadrantDashboard);
    public Type? PresentationComponentType => typeof(QuadrantPresentation);
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;

    // Quadrant dashboard and presentation both need ActivityConfig in addition
    // to the common parameters.

    public IDictionary<string, object?> BuildDashboardParameters(
        string sessionCode, Guid activityId, string? activityConfig)
        => new Dictionary<string, object?>
        {
            { "SessionCode",    sessionCode },
            { "ActivityId",     activityId },
            { "ActivityConfig", activityConfig },
        };

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
            { "SessionCode",     sessionCode },
            { "ActivityId",      activityId },
            { "ActivityTitle",   activityTitle },
            { "ActivityPrompt",  activityPrompt },
            { "DurationMinutes", durationMinutes },
            { "OpenedAt",        openedAt },
            { "ActivityConfig",  activityConfig },
        };
}
