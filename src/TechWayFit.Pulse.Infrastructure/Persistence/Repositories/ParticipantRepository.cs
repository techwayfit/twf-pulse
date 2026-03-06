using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB ParticipantRepository implementation.
/// </summary>
public sealed class ParticipantRepository : IParticipantRepository
{
    private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

    public ParticipantRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
    _dbContextFactory = dbContextFactory;
    }

    private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Participants
          .AsNoTracking()
    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

  public async Task<IReadOnlyList<Participant>> GetBySessionAsync(
        Guid sessionId,
 CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Participants
         .AsNoTracking()
    .Where(x => x.SessionId == sessionId)
    .OrderBy(x => x.JoinedAt)
        .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task AddAsync(Participant participant, CancellationToken cancellationToken = default)
 {
     await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.Participants.Add(participant.ToRecord());
  await dbContext.SaveChangesAsync(cancellationToken);
    }
}
