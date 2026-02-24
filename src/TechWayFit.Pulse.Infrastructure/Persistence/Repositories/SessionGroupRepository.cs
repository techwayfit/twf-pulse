using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared SessionGroupRepository implementation for all providers.
/// </summary>
public class SessionGroupRepository<TContext> : ISessionGroupRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    public SessionGroupRepository(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<SessionGroup> CreateAsync(SessionGroup group, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
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

        dbContext.SessionGroups.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return group;
    }

    public async Task<SessionGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.SessionGroups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        return record != null ? MapToDomain(record) : null;
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetByFacilitatorAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.SessionGroups
            .Where(g => g.FacilitatorUserId == facilitatorUserId)
            .OrderBy(g => g.Level)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetByParentAsync(Guid parentGroupId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.SessionGroups
            .Where(g => g.ParentGroupId == parentGroupId)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<SessionGroup> UpdateAsync(SessionGroup group, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.SessionGroups
            .FirstOrDefaultAsync(g => g.Id == group.Id, cancellationToken);

        if (record == null)
        {
            throw new InvalidOperationException($"SessionGroup with ID {group.Id} not found.");
        }

        record.Name = group.Name;
        record.Description = group.Description;
        record.UpdatedAt = group.UpdatedAt;
        record.Icon = group.Icon;
        record.Color = group.Color;

        await dbContext.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.SessionGroups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (record != null)
        {
            dbContext.SessionGroups.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> HasChildGroupsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.SessionGroups
            .AnyAsync(g => g.ParentGroupId == groupId, cancellationToken);
    }

    public async Task<bool> HasSessionsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.Sessions
            .AnyAsync(s => s.GroupId == groupId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SessionGroup>> GetHierarchyAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.SessionGroups
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
