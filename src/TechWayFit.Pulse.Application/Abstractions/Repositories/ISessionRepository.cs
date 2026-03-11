using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetByFacilitatorUserIdAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
        Guid facilitatorUserId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Session>> GetByGroupAsync(Guid? groupId, Guid facilitatorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets lightweight session summaries for a facilitator.
    /// Optimized for dashboard widgets - only returns essential fields without heavy navigation properties.
    /// </summary>
    /// <param name="facilitatorUserId">The facilitator user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of lightweight session summaries</returns>
    Task<IReadOnlyList<DTOs.SessionSummaryDto>> GetSessionSummariesByFacilitatorAsync(
        Guid facilitatorUserId, 
        CancellationToken cancellationToken = default);

    Task AddAsync(Session session, CancellationToken cancellationToken = default);

    Task UpdateAsync(Session session, CancellationToken cancellationToken = default);
}
