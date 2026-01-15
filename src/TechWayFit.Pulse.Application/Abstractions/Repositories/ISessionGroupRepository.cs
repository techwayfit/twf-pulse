using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface ISessionGroupRepository
{
    Task<SessionGroup> CreateAsync(SessionGroup group, CancellationToken cancellationToken = default);
    Task<SessionGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SessionGroup>> GetByFacilitatorAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SessionGroup>> GetByParentAsync(Guid parentGroupId, CancellationToken cancellationToken = default);
    Task<SessionGroup> UpdateAsync(SessionGroup group, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasChildGroupsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<bool> HasSessionsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SessionGroup>> GetHierarchyAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
}