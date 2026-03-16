using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Activities.Abstractions;

/// <summary>
/// Unified dashboard service that replaces the per-activity-type dashboard service interfaces
/// (<c>IPollDashboardService</c>, <c>IWordCloudDashboardService</c>, etc.).
/// <para>
/// Delegates to the appropriate <see cref="IActivityPlugin"/> via <see cref="IActivityRegistry"/>.
/// The caller receives an <see cref="IActivityDashboardData"/> and casts to the concrete
/// dashboard data type they expect (e.g. <c>PollDashboardData</c>).
/// </para>
/// </summary>
public interface IActivityDashboardService
{
    /// <summary>
    /// Returns live dashboard data for any registered activity type.
    /// </summary>
    /// <param name="activityType">The activity type. Used to look up the correct plugin.</param>
    /// <param name="sessionId">The session that owns the activity.</param>
    /// <param name="activityId">The specific activity to compute data for.</param>
    /// <param name="filters">Participant dimension filters (e.g. role=developer).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IActivityDashboardData> GetDashboardDataAsync(
        ActivityType activityType,
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default);
}
