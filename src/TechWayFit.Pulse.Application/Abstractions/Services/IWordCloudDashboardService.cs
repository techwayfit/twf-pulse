using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IWordCloudDashboardService
{
    Task<WordCloudDashboardResponse> GetWordCloudDashboardAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default);
}