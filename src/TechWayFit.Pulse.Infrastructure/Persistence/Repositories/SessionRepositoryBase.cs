using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for Session with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class SessionRepositoryBase : ISessionRepository
{
    protected readonly IPulseDbContext _dbContext;

protected SessionRepositoryBase(IPulseDbContext dbContext)
    {
  _dbContext = dbContext;
    }

    // ? Shared implementation - no duplication
    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Sessions
  .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    // ? Shared implementation - no duplication
 public async Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Sessions
            .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

  return record?.ToDomain();
    }

    // ? Shared implementation - no duplication
    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        _dbContext.Sessions.Add(session.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ? Shared implementation - no duplication
    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        var existingRecord = await _dbContext.Sessions.FindAsync(new object[] { session.Id }, cancellationToken);

      if (existingRecord == null)
        {
            throw new InvalidOperationException($"Session with ID {session.Id} not found.");
     }

        var updatedRecord = session.ToRecord();
        _dbContext.Entry(existingRecord).CurrentValues.SetValues(updatedRecord);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyList<Session>> GetByFacilitatorUserIdAsync(
   Guid facilitatorUserId,
  CancellationToken cancellationToken = default)
    {
   var query = _dbContext.Sessions
      .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId);

        // Apply provider-specific sorting
   query = ApplySorting(query, descending: true);

 var records = await query.ToListAsync(cancellationToken);
  return records.Select(r => r.ToDomain()).ToList();
    }

    // ? Template method - uses virtual ApplySorting (can be fully overridden for SQL Server optimization)
    public virtual async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
        Guid facilitatorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sessions
            .AsNoTracking()
   .Where(x => x.FacilitatorUserId == facilitatorUserId);

        var totalCount = await query.CountAsync(cancellationToken);

   // Apply provider-specific sorting
        query = ApplySorting(query, descending: true);

      // Apply pagination
        var records = await query
     .Skip((page - 1) * pageSize)
      .Take(pageSize)
    .ToListAsync(cancellationToken);

        var sessions = records.Select(r => r.ToDomain()).ToList();
      return (sessions, totalCount);
    }

    // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyCollection<Session>> GetByGroupAsync(
      Guid? groupId,
        Guid facilitatorUserId,
     CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sessions
          .AsNoTracking()
          .Where(x => x.FacilitatorUserId == facilitatorUserId && x.GroupId == groupId);

    // Apply provider-specific sorting
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
  // Default: Server-side sorting (works for most providers)
        return descending
   ? query.OrderByDescending(x => x.CreatedAt)
            : query.OrderBy(x => x.CreatedAt);
    }
}
