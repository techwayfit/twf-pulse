using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized SessionRepository with server-side sorting and pagination.
/// </summary>
public sealed class SessionRepository : SessionRepositoryBase<PulseMariaDbContext>
{
    public SessionRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    // ? Override: MariaDB-specific pagination with LIMIT/OFFSET
    public override async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
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

        var records = await ApplySorting(query, descending: true)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (records.Select(r => r.ToDomain()).ToList(), totalCount);
    }
}
