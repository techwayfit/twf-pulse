using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.Sqlite;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific ParticipantRepository.
/// SQLite does not support DateTimeOffset in ORDER BY clauses, so sorted
/// queries materialize results first and then apply client-side ordering.
/// </summary>
public sealed class ParticipantRepository : ParticipantRepositoryBase<PulseSqlLiteDbContext>
{
    public ParticipantRepository(IDbContextFactory<PulseSqlLiteDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    public override async Task<IReadOnlyList<Participant>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Participants
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        return records
            .OrderBy(x => x.JoinedAt)
            .Select(r => r.ToDomain())
            .ToList();
    }
}
