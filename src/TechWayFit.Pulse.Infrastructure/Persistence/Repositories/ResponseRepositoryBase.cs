using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for Response with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class ResponseRepositoryBase : IResponseRepository
{
    protected readonly IPulseDbContext _dbContext;

    protected ResponseRepositoryBase(IPulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ? Shared implementation - no duplication
    public async Task AddAsync(Response response, CancellationToken cancellationToken = default)
    {
        _dbContext.Responses.Add(response.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

  // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyList<Response>> GetByActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Responses
.AsNoTracking()
            .Where(x => x.ActivityId == activityId);

        // Apply provider-specific sorting
    query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(record => record.ToDomain()).ToList();
    }

    // ? Shared implementation - no duplication
    public Task<int> CountByActivityAndParticipantAsync(
    Guid activityId,
  Guid participantId,
        CancellationToken cancellationToken = default)
{
        return _dbContext.Responses
.AsNoTracking()
  .Where(x => x.ActivityId == activityId && x.ParticipantId == participantId)
        .CountAsync(cancellationToken);
    }

    // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyList<Response>> GetByParticipantAsync(
      Guid sessionId,
        Guid participantId,
     CancellationToken cancellationToken = default)
  {
      var query = _dbContext.Responses
            .AsNoTracking()
     .Where(x => x.SessionId == sessionId && x.ParticipantId == participantId);

        // Apply provider-specific sorting
      query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
      return records.Select(record => record.ToDomain()).ToList();
    }

    // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyList<Response>> GetBySessionAsync(
  Guid sessionId,
        CancellationToken cancellationToken = default)
    {
      var query = _dbContext.Responses
     .AsNoTracking()
    .Where(x => x.SessionId == sessionId);

        // Apply provider-specific sorting
        query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
      return records.Select(record => record.ToDomain()).ToList();
    }

    /// <summary>
    /// Virtual method for provider-specific sorting implementation.
    /// Override in derived classes for optimal performance.
    /// </summary>
    protected virtual IQueryable<Entities.ResponseRecord> ApplySorting(
        IQueryable<Entities.ResponseRecord> query)
    {
        // Default: Server-side sorting (works for most providers)
  return query.OrderBy(x => x.CreatedAt);
    }
}
