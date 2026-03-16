using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Rating;

/// <summary>
/// Maps the Rating activity type to its three Blazor components:
/// participant input, facilitator dashboard, and presentation view.
/// </summary>
public sealed class RatingActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.Rating;
    public Type? ParticipantComponentType  => typeof(RatingActivity);
    public Type? DashboardComponentType    => typeof(RatingDashboard);
    public Type? PresentationComponentType => typeof(RatingPresentation);
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;

    // RatingPresentation requires MaxRating in addition to the common parameters.
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
            { "MaxRating",       5 },
        };
}
