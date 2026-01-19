using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IGeneralFeedbackDashboardService
{
    Task<GeneralFeedbackDashboardResponse> GetGeneralFeedbackDashboardAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default);
}
