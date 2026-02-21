using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for Participant with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class ParticipantRepositoryBase : IParticipantRepository
{
    protected readonly IPulseDbContext _dbContext;

    protected ParticipantRepositoryBase(IPulseDbContext dbContext)
 {
        _dbContext = dbContext;
    }

    // ? Shared implementation - no duplication
    public async Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Participants
         .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

return record?.ToDomain();
    }

    // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyList<Participant>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Participants
     .AsNoTracking()
  .Where(x => x.SessionId == sessionId);

        // Apply provider-specific sorting
 query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(record => record.ToDomain()).ToList();
  }

    // ? Shared implementation - no duplication
    public async Task AddAsync(Participant participant, CancellationToken cancellationToken = default)
    {
    _dbContext.Participants.Add(participant.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Virtual method for provider-specific sorting implementation.
    /// Override in derived classes for optimal performance.
    /// </summary>
    protected virtual IQueryable<Entities.ParticipantRecord> ApplySorting(
        IQueryable<Entities.ParticipantRecord> query)
    {
        // Default: Server-side sorting (works for most providers)
      return query.OrderBy(x => x.JoinedAt);
    }
}
