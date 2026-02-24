using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.Sqlite;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific ResponseRepository.
/// SQLite does not support DateTimeOffset in ORDER BY clauses, so sorted
/// queries materialize results first and then apply client-side ordering.
/// </summary>
public sealed class ResponseRepository : ResponseRepositoryBase<PulseSqlLiteDbContext>
{
    public ResponseRepository(IDbContextFactory<PulseSqlLiteDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    public override async Task<IReadOnlyList<Response>> GetByActivityAsync(
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Responses
            .AsNoTracking()
            .Where(x => x.ActivityId == activityId)
            .ToListAsync(cancellationToken);

        return records
            .OrderBy(x => x.CreatedAt)
            .Select(r => r.ToDomain())
            .ToList();
    }

    public override async Task<IReadOnlyList<Response>> GetByParticipantAsync(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Responses
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId && x.ParticipantId == participantId)
            .ToListAsync(cancellationToken);

        return records
            .OrderBy(x => x.CreatedAt)
            .Select(r => r.ToDomain())
            .ToList();
    }

    public override async Task<IReadOnlyList<Response>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Responses
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        return records
            .OrderBy(x => x.CreatedAt)
            .Select(r => r.ToDomain())
            .ToList();
    }
}
