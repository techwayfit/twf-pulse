using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared SessionGroupRepository implementation.
/// </summary>
public class SessionGroupRepository : ISessionGroupRepository
{
    protected readonly IPulseDbContext _context;

    public SessionGroupRepository(IPulseDbContext context)
    {
        _context = context;
    }

    public async Task<SessionGroup> CreateAsync(SessionGroup group, CancellationToken cancellationToken = default)
    {
        var record = new SessionGroupRecord
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Level = group.Level,
            ParentGroupId = group.ParentGroupId,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            FacilitatorUserId = group.FacilitatorUserId,
            Icon = group.Icon,
            Color = group.Color,
            IsDefault = group.IsDefault
        };

        _context.SessionGroups.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        return group;
    }

    public async Task<SessionGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _context.SessionGroups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        return record != null ? MapToDomain(record) : null;
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetByFacilitatorAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        var records = await _context.SessionGroups
            .Where(g => g.FacilitatorUserId == facilitatorUserId)
            .OrderBy(g => g.Level)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetByParentAsync(Guid parentGroupId, CancellationToken cancellationToken = default)
    {
        var records = await _context.SessionGroups
            .Where(g => g.ParentGroupId == parentGroupId)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<SessionGroup> UpdateAsync(SessionGroup group, CancellationToken cancellationToken = default)
    {
        var record = await _context.SessionGroups
            .FirstOrDefaultAsync(g => g.Id == group.Id, cancellationToken);

        if (record == null)
            throw new InvalidOperationException($"SessionGroup with ID {group.Id} not found.");

        record.Name = group.Name;
        record.Description = group.Description;
        record.UpdatedAt = group.UpdatedAt;
        record.Icon = group.Icon;
        record.Color = group.Color;

        await _context.SaveChangesAsync(cancellationToken);

        return group;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _context.SessionGroups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (record != null)
        {
            _context.SessionGroups.Remove(record);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> HasChildGroupsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.SessionGroups
            .AnyAsync(g => g.ParentGroupId == groupId, cancellationToken);
    }

    public async Task<bool> HasSessionsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AnyAsync(s => s.GroupId == groupId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetHierarchyAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        var records = await _context.SessionGroups
            .Where(g => g.FacilitatorUserId == facilitatorUserId)
            .OrderBy(g => g.Level)
            .ThenBy(g => g.ParentGroupId)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    private static SessionGroup MapToDomain(SessionGroupRecord record)
    {
        return new SessionGroup(
            record.Id,
            record.Name,
            record.Description,
            record.Level,
            record.ParentGroupId,
            record.CreatedAt,
            record.UpdatedAt,
            record.FacilitatorUserId,
            record.Icon,
            record.Color,
            record.IsDefault);
    }
}