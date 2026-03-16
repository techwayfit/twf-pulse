using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.AiSummary;

/// <summary>
/// Maps the AiSummary activity type to its Blazor components.
/// AiSummary has no presentation view — participants read the AI-generated summary only.
/// </summary>
public sealed class AiSummaryActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.AiSummary;
    public Type? ParticipantComponentType  => typeof(AiSummaryActivity);
    public Type? DashboardComponentType    => typeof(AiSummaryDashboard);
    public Type? PresentationComponentType => null;
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;
}
