using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Services;

public sealed class SessionGroupService : ISessionGroupService
{
    private readonly ISessionGroupRepository _groups;
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 1000;

    public SessionGroupService(ISessionGroupRepository groups)
    {
        _groups = groups;
    }

    public async Task<SessionGroup> CreateGroupAsync(
        string name,
        string? description,
        int level,
        Guid? parentGroupId,
        DateTimeOffset now,
        Guid facilitatorUserId,
        string? icon = null,
        string? color = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.", nameof(name));

        if (name.Trim().Length > NameMaxLength)
            throw new ArgumentException($"Group name must be <= {NameMaxLength} characters.", nameof(name));

        if (!string.IsNullOrEmpty(description) && description.Trim().Length > DescriptionMaxLength)
            throw new ArgumentException($"Group description must be <= {DescriptionMaxLength} characters.", nameof(description));

        // Validate parent group exists if specified
        if (parentGroupId.HasValue)
        {
            var parentGroup = await _groups.GetByIdAsync(parentGroupId.Value, cancellationToken);
            if (parentGroup == null)
                throw new ArgumentException("Parent group not found.", nameof(parentGroupId));

            if (parentGroup.FacilitatorUserId != facilitatorUserId)
                throw new UnauthorizedAccessException("Cannot create group under another facilitator's group.");

            // Validate level hierarchy
            if (level != parentGroup.Level + 1)
                throw new ArgumentException($"Invalid level. Expected level {parentGroup.Level + 1} for this parent group.", nameof(level));
        }

        var group = new SessionGroup(
            Guid.NewGuid(),
            name,
            description,
            level,
            parentGroupId,
            now,
            now,
            facilitatorUserId,
            icon,
            color,
            false); // Not a default group

        return await _groups.CreateAsync(group, cancellationToken);
    }

    public async Task<SessionGroup?> GetGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _groups.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetFacilitatorGroupsAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        return await _groups.GetByFacilitatorAsync(facilitatorUserId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetChildGroupsAsync(
        Guid parentGroupId,
        CancellationToken cancellationToken = default)
    {
        return await _groups.GetByParentAsync(parentGroupId, cancellationToken);
    }

    public async Task<SessionGroup> UpdateGroupAsync(
        Guid id,
        string name,
        string? description,
        DateTimeOffset now,
        string? icon = null,
        string? color = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.", nameof(name));

        if (name.Trim().Length > NameMaxLength)
            throw new ArgumentException($"Group name must be <= {NameMaxLength} characters.", nameof(name));

        if (!string.IsNullOrEmpty(description) && description.Trim().Length > DescriptionMaxLength)
            throw new ArgumentException($"Group description must be <= {DescriptionMaxLength} characters.", nameof(description));

        var group = await _groups.GetByIdAsync(id, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found.");

        group.Update(name, description, now, icon, color);
        return await _groups.UpdateAsync(group, cancellationToken);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(id, cancellationToken);
        if (group == null)
            return; // Already deleted or doesn't exist

        // Check if group has child groups
        var hasChildGroups = await _groups.HasChildGroupsAsync(id, cancellationToken);
        if (hasChildGroups)
            throw new InvalidOperationException("Cannot delete group that has child groups. Delete child groups first.");

        // Check if group has sessions
        var hasSessions = await _groups.HasSessionsAsync(id, cancellationToken);
        if (hasSessions)
            throw new InvalidOperationException("Cannot delete group that has sessions. Move or delete sessions first.");

        await _groups.DeleteAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetGroupHierarchyAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        return await _groups.GetHierarchyAsync(facilitatorUserId, cancellationToken);
    }

    public async Task<SessionGroup> CreateDefaultGroupAsync(
        Guid facilitatorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var group = new SessionGroup(
            Guid.NewGuid(),
            "Default",
            "Default group for organizing sessions",
            1,
            null,
            now,
            now,
            facilitatorUserId,
            "📁",
            null,
            true); // Mark as default group

        return await _groups.CreateAsync(group, cancellationToken);
    }

    public async Task<SessionGroup?> GetDefaultGroupAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        var groups = await _groups.GetByFacilitatorAsync(facilitatorUserId, cancellationToken);
        return groups.FirstOrDefault(g => g.IsDefault);
    }
}