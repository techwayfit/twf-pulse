using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for Response with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class ResponseRepositoryBase<TContext> : IResponseRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    protected ResponseRepositoryBase(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task AddAsync(Response response, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.Responses.Add(response.ToRecord());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Response>> GetByActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.Responses
            .AsNoTracking()
            .Where(x => x.ActivityId == activityId);

        query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(record => record.ToDomain()).ToList();
    }

    public async Task<int> CountByActivityAndParticipantAsync(
        Guid activityId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.Responses
            .AsNoTracking()
            .Where(x => x.ActivityId == activityId && x.ParticipantId == participantId)
            .CountAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Response>> GetByParticipantAsync(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.Responses
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId && x.ParticipantId == participantId);

        query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(record => record.ToDomain()).ToList();
    }

    public virtual async Task<IReadOnlyList<Response>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.Responses
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId);

        query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(record => record.ToDomain()).ToList();
    }

    protected virtual IQueryable<Entities.ResponseRecord> ApplySorting(
        IQueryable<Entities.ResponseRecord> query)
    {
        return query.OrderBy(x => x.CreatedAt);
    }
}
