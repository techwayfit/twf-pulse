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
    protected readonly TContext DbContext;

    protected ResponseRepositoryBase(TContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task AddAsync(Response response, CancellationToken cancellationToken = default)
    {
        DbContext.Responses.Add(response.ToRecord());
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Response>> GetByActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Responses
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
        return await DbContext.Responses
            .AsNoTracking()
            .Where(x => x.ActivityId == activityId && x.ParticipantId == participantId)
            .CountAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Response>> GetByParticipantAsync(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Responses
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
        var query = DbContext.Responses
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

    public async Task UpdatePayloadAsync(
        Guid responseId,
        string newPayload,
        CancellationToken cancellationToken = default)
    {
        var record = await DbContext.Responses.FindAsync(new object[] { responseId }, cancellationToken);
        if (record is null) return;
        record.PayloadJson = newPayload;
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Response?> GetByIdAsync(
        Guid responseId,
        CancellationToken cancellationToken = default)
    {
        var record = await DbContext.Responses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == responseId, cancellationToken);
        return record?.ToDomain();
    }
}
