using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(
        Guid sessionId,
        Guid? activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default);
}
