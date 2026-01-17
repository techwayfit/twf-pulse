using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IPollDashboardService
{
    Task<PollDashboardResponse> GetPollDashboardAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default);
}