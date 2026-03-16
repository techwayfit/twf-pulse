using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Activities.Registry;

/// <summary>
/// Unified dashboard service that replaces <c>IPollDashboardService</c>,
/// <c>IWordCloudDashboardService</c>, <c>IRatingDashboardService</c>, 
/// <c>IGeneralFeedbackDashboardService</c>, <c>IQnADashboardService</c>,
/// <c>IQuadrantDashboardService</c>, and any future per-activity dashboard services.
/// <para>
/// Delegates to the registered <see cref="IActivityPlugin"/> for the given type.
/// The returned <see cref="IActivityDashboardData"/> is the plugin's concrete data type;
/// callers in the API / Blazor layer cast to the expected type.
/// </para>
/// </summary>
public sealed class ActivityDashboardService : IActivityDashboardService
{
    private readonly IActivityRegistry _registry;
    private readonly IActivityDataContext _dataContext;

    public ActivityDashboardService(IActivityRegistry registry, IActivityDataContext dataContext)
    {
        _registry    = registry;
        _dataContext = dataContext;
    }

    /// <inheritdoc />
    public Task<IActivityDashboardData> GetDashboardDataAsync(
        ActivityType activityType,
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default)
    {
        var plugin = _registry.GetPlugin(activityType);
        return plugin.GetDashboardDataAsync(sessionId, activityId, filters, _dataContext, cancellationToken);
    }
}
