using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.WordCloud;

/// <summary>
/// Maps the WordCloud activity type to its three Blazor components:
/// participant input, facilitator dashboard, and presentation view.
/// </summary>
public sealed class WordCloudActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.WordCloud;
    public Type? ParticipantComponentType  => typeof(WordCloudActivity);
    public Type? DashboardComponentType    => typeof(WordCloudDashboard);
    public Type? PresentationComponentType => typeof(WordCloudPresentation);
    public Type? EditConfigComponentType   => null;
    public Type? CreateModalComponentType  => null;
}
