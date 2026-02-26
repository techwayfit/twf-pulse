using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IQnADashboardService
{
    Task<QnADashboardResponse> GetQnADashboardAsync(
        Guid sessionId,
        Guid activityId,
        CancellationToken cancellationToken = default);
}
