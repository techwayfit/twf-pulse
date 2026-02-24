using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized SessionRepository with server-side sorting and true database pagination.
/// Overrides base class methods for SQL Server-specific optimizations.
/// </summary>
public sealed class SessionRepository : SessionRepositoryBase<PulseSqlServerDbContext>
{
    public SessionRepository(IDbContextFactory<PulseSqlServerDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    // ? Override: SQL Server-specific pagination with OFFSET/FETCH
    public override async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
Guid facilitatorUserId,
        int page,
     int pageSize,
        CancellationToken cancellationToken = default)
    {
// SQL Server optimization: True database pagination with OFFSET/FETCH
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Server-side sorting and pagination - only loads requested page
        var records = await ApplySorting(query, descending: true)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (records.Select(r => r.ToDomain()).ToList(), totalCount);
    }

    // ? Inherits all other methods from SessionRepositoryBase (GetByIdAsync, GetByCodeAsync, AddAsync, UpdateAsync, GetByFacilitatorUserIdAsync, GetByGroupAsync)
    // ? Uses base class ApplySorting (server-side sorting works great in SQL Server)
}
