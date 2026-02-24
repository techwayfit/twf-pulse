using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for Session with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class SessionRepositoryBase<TContext> : ISessionRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    protected SessionRepositoryBase(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        return record?.ToDomain();
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.Sessions.Add(session.ToRecord());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var existingRecord = await dbContext.Sessions.FindAsync(new object[] { session.Id }, cancellationToken);

        if (existingRecord == null)
        {
            throw new InvalidOperationException($"Session with ID {session.Id} not found.");
        }

        var updatedRecord = session.ToRecord();
        dbContext.Entry(existingRecord).CurrentValues.SetValues(updatedRecord);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Session>> GetByFacilitatorUserIdAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId);

        query = ApplySorting(query, descending: true);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public virtual async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
        Guid facilitatorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, descending: true);

        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var sessions = records.Select(r => r.ToDomain()).ToList();
        return (sessions, totalCount);
    }

    public virtual async Task<IReadOnlyCollection<Session>> GetByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId && x.GroupId == groupId);

        query = ApplySorting(query, descending: true);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(r => r.ToDomain()).ToList();
    }

    /// <summary>
    /// Virtual method for provider-specific sorting implementation.
    /// Override in derived classes for optimal performance.
    /// </summary>
    protected virtual IQueryable<Entities.SessionRecord> ApplySorting(
        IQueryable<Entities.SessionRecord> query,
        bool descending = true)
    {
        return descending
            ? query.OrderByDescending(x => x.CreatedAt)
            : query.OrderBy(x => x.CreatedAt);
    }
}
