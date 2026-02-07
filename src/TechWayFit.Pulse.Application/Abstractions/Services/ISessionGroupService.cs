using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface ISessionGroupService
{
    Task<SessionGroup> CreateGroupAsync(
        string name,
        string? description,
        int level,
        Guid? parentGroupId,
        DateTimeOffset now,
        Guid facilitatorUserId,
        string? icon = null,
        string? color = null,
        CancellationToken cancellationToken = default);

    Task<SessionGroup?> GetGroupAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SessionGroup>> GetFacilitatorGroupsAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SessionGroup>> GetChildGroupsAsync(
        Guid parentGroupId,
        CancellationToken cancellationToken = default);

    Task<SessionGroup> UpdateGroupAsync(
        Guid id,
        string name,
        string? description,
        DateTimeOffset now,
        string? icon = null,
        string? color = null,
        CancellationToken cancellationToken = default);

    Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SessionGroup>> GetGroupHierarchyAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default);

    Task<SessionGroup> CreateDefaultGroupAsync(
        Guid facilitatorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<SessionGroup?> GetDefaultGroupAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default);
}