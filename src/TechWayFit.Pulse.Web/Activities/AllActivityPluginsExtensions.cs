using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities;
using TechWayFit.Pulse.Application.Activities.Plugins.AiSummary;
using TechWayFit.Pulse.Application.Activities.Plugins.Break;
using TechWayFit.Pulse.Application.Activities.Plugins.FiveWhys;
using TechWayFit.Pulse.Application.Activities.Plugins.GeneralFeedback;
using TechWayFit.Pulse.Application.Activities.Plugins.Poll;
using TechWayFit.Pulse.Application.Activities.Plugins.QnA;
using TechWayFit.Pulse.Application.Activities.Plugins.Quadrant;
using TechWayFit.Pulse.Application.Activities.Plugins.Rating;
using TechWayFit.Pulse.Application.Activities.Plugins.WordCloud;
using TechWayFit.Pulse.Web.Activities.Plugins.AiSummary;
using TechWayFit.Pulse.Web.Activities.Plugins.Break;
using TechWayFit.Pulse.Web.Activities.Plugins.FiveWhys;
using TechWayFit.Pulse.Web.Activities.Plugins.GeneralFeedback;
using TechWayFit.Pulse.Web.Activities.Plugins.Poll;
using TechWayFit.Pulse.Web.Activities.Plugins.QnA;
using TechWayFit.Pulse.Web.Activities.Plugins.Quadrant;
using TechWayFit.Pulse.Web.Activities.Plugins.Rating;
using TechWayFit.Pulse.Web.Activities.Plugins.WordCloud;

namespace TechWayFit.Pulse.Web.Activities;

/// <summary>
/// Convenience extension that wires up every built-in activity plugin in one call.
/// <para>
/// Registers the Application-layer plugin registry (<see cref="IActivityRegistry"/>) and
/// the Web-layer UI registry (<see cref="IActivityUiRegistry"/>) together with all nine
/// concrete activity plugins.
/// </para>
/// <para>
/// Call once from <c>Program.cs</c> after infrastructure services are registered:
/// <code>builder.Services.AddAllActivityPlugins();</code>
/// </para>
/// </summary>
public static class AllActivityPluginsExtensions
{
    /// <summary>
    /// Registers all activity plugins (Application + Web layers) in a single call.
    /// </summary>
    public static IServiceCollection AddAllActivityPlugins(this IServiceCollection services)
    {
        // ── Application-layer plugins (IActivityPlugin singletons) ────────────
        services
            .AddPollActivity()
            .AddWordCloudActivity()
            .AddRatingActivity()
            .AddQnAActivity()
            .AddQuadrantActivity()
            .AddGeneralFeedbackActivity()
            .AddFiveWhysActivity()
            .AddAiSummaryActivity()
            .AddBreakActivity();

        // ── Application-layer registry + dashboard service infrastructure ─────
        // Must be called AFTER all IActivityPlugin registrations so the
        // ActivityRegistry constructor sees them via IEnumerable<IActivityPlugin>.
        services.AddActivityPluginRegistry();

        // ── Web-layer UI descriptors (IActivityUiDescriptor singletons) ───────
        services.AddActivityUi();

        services
            .AddPollActivityUi()
            .AddWordCloudActivityUi()
            .AddRatingActivityUi()
            .AddQnAActivityUi()
            .AddQuadrantActivityUi()
            .AddGeneralFeedbackActivityUi()
            .AddFiveWhysActivityUi()
            .AddAiSummaryActivityUi()
            .AddBreakActivityUi();

        return services;
    }
}
