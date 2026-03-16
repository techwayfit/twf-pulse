using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.FiveWhys;

/// <summary>
/// Maps the FiveWhys activity type to its three Blazor components:
/// participant input, facilitator dashboard, and presentation view.
/// </summary>
public sealed class FiveWhysActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.FiveWhys;
    public Type? ParticipantComponentType  => typeof(FiveWhysActivity);
    public Type? DashboardComponentType    => typeof(FiveWhysDashboard);
    public Type? PresentationComponentType => typeof(FiveWhysPresentation);
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;
}
