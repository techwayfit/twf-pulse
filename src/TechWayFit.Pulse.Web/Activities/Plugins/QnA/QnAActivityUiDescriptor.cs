using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.QnA;

/// <summary>
/// Maps the QnA activity type to its three Blazor components:
/// participant input, facilitator dashboard, and presentation view.
/// </summary>
public sealed class QnAActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.QnA;
    public Type? ParticipantComponentType  => typeof(QnAActivity);
    public Type? DashboardComponentType    => typeof(QnADashboard);
    public Type? PresentationComponentType => typeof(QnAPresentation);
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;
}
