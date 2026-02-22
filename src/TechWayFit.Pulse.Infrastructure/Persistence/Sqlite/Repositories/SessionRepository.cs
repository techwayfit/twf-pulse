using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific SessionRepository.
/// SQLite does not support DateTimeOffset in ORDER BY clauses, so all sorted
/// queries materialize results first and then apply client-side ordering.
/// </summary>
public sealed class SessionRepository : SessionRepositoryBase
{
    public SessionRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }

    public override async Task<IReadOnlyList<Session>> GetByFacilitatorUserIdAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId)
            .ToListAsync(cancellationToken);

        return records
            .OrderByDescending(x => x.CreatedAt)
            .Select(r => r.ToDomain())
            .ToList();
    }

    public override async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
        Guid facilitatorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var allRecords = await _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId)
            .ToListAsync(cancellationToken);

        var totalCount = allRecords.Count;

        var sessions = allRecords
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.ToDomain())
            .ToList();

        return (sessions, totalCount);
    }

    public override async Task<IReadOnlyCollection<Session>> GetByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId && x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        return records
            .OrderByDescending(x => x.CreatedAt)
            .Select(r => r.ToDomain())
            .ToList();
    }
}
