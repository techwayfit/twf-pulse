using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific FacilitatorUserRepository.
/// SQLite does not support DateTimeOffset in ORDER BY clauses, so sorted
/// queries materialize results first and then apply client-side ordering.
/// </summary>
public sealed class FacilitatorUserRepository : FacilitatorUserRepositoryBase
{
    public FacilitatorUserRepository(IPulseDbContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<FacilitatorUser>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await _context.FacilitatorUsers
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return records
            .OrderBy(u => u.CreatedAt)
            .Select(MapToDomain)
            .ToList();
    }
}
