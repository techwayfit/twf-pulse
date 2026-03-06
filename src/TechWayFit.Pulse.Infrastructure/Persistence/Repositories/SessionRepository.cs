using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB SessionRepository with server-side sorting and pagination.
/// </summary>
public sealed class SessionRepository : ISessionRepository
{
    private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

    public SessionRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<Session>> GetByFacilitatorUserIdAsync(
  Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Sessions
  .AsNoTracking()
         .Where(x => x.FacilitatorUserId == facilitatorUserId)
            .OrderByDescending(x => x.CreatedAt)
      .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
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

        var records = await query
        .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
     .Take(pageSize)
      .ToListAsync(cancellationToken);

        return (records.Select(r => r.ToDomain()).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<Session>> GetByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Sessions
          .AsNoTracking()
              .Where(x => x.GroupId == groupId)
         .OrderByDescending(x => x.CreatedAt)
      .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Session>> GetByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Sessions
            .AsNoTracking()
   .Where(x => x.FacilitatorUserId == facilitatorUserId
            && (groupId == null ? x.GroupId == null : x.GroupId == groupId))
  .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Session>> GetExpiredAsync(
    DateTimeOffset before,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Sessions
            .AsNoTracking()
         .Where(x => x.ExpiresAt < before)
            .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.Sessions.AnyAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<int> CountActiveByFacilitatorAsync(
        Guid facilitatorUserId,
        DateTimeOffset since,
 CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.Sessions
   .AsNoTracking()
      .Where(x => x.FacilitatorUserId == facilitatorUserId
           && x.CreatedAt >= since
       && x.Status != (int)SessionStatus.Ended)
    .CountAsync(cancellationToken);
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
        var record = await dbContext.Sessions.FindAsync(new object[] { session.Id }, cancellationToken);
        if (record is null)
        {
            throw new InvalidOperationException($"Session with ID {session.Id} not found.");
        }

        record.Code = session.Code;
        record.Title = session.Title;
        record.Goal = session.Goal;
        record.ContextJson = session.Context;
        record.SettingsJson = PersistenceJson.SerializeSessionSettings(session.Settings);
        record.JoinFormSchemaJson = PersistenceJson.SerializeJoinFormSchema(session.JoinFormSchema);
        record.Status = (int)session.Status;
        record.CurrentActivityId = session.CurrentActivityId;
        record.UpdatedAt = session.UpdatedAt;
        record.ExpiresAt = session.ExpiresAt;
        record.FacilitatorUserId = session.FacilitatorUserId;
        record.GroupId = session.GroupId;
        record.SessionStart = session.SessionStart;
        record.SessionEnd = session.SessionEnd;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Session session, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Sessions.FindAsync(new object[] { session.Id }, cancellationToken);
        if (record != null)
        {
            dbContext.Sessions.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteBulkAsync(
        IEnumerable<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        await dbContext.Sessions
          .Where(x => sessionIds.Contains(x.Id))
       .ExecuteDeleteAsync(cancellationToken);
    }
}
