using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Poll;

/// <summary>
/// Maps the Poll activity type to its three Blazor components:
/// participant input, facilitator dashboard, and presentation view.
/// </summary>
public sealed class PollActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.Poll;

    public Type ParticipantComponentType => typeof(PollActivity);

    public Type DashboardComponentType => typeof(PollDashboard);

    public Type? PresentationComponentType => typeof(PollPresentation);

    // Edit config and create-modal components are not yet extracted into
    // dedicated components; return null to fall through to the default form.
    public Type? EditConfigComponentType    => null;
    public Type? CreateModalComponentType   => null;
}
