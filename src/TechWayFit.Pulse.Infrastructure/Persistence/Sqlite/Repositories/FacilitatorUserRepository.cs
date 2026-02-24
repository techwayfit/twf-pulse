using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.Sqlite;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific FacilitatorUserRepository.
/// SQLite does not support DateTimeOffset in ORDER BY clauses, so sorted
/// queries materialize results first and then apply client-side ordering.
/// </summary>
public sealed class FacilitatorUserRepository : FacilitatorUserRepositoryBase<PulseSqlLiteDbContext>
{
    public FacilitatorUserRepository(IDbContextFactory<PulseSqlLiteDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    public override async Task<IReadOnlyList<FacilitatorUser>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.FacilitatorUsers
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return records
            .OrderBy(u => u.CreatedAt)
            .Select(MapToDomain)
            .ToList();
    }
}
