using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.GeneralFeedback;

/// <summary>
/// Maps the GeneralFeedback activity type to its three Blazor components:
/// participant input, facilitator dashboard, and presentation view.
/// </summary>
public sealed class GeneralFeedbackActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.GeneralFeedback;
    public Type? ParticipantComponentType  => typeof(GeneralFeedbackActivity);
    public Type? DashboardComponentType    => typeof(GeneralFeedbackDashboard);
    public Type? PresentationComponentType => typeof(GeneralFeedbackPresentation);
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;
}
