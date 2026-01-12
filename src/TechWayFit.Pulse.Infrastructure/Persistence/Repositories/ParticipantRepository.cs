using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

public sealed class ParticipantRepository : IParticipantRepository
{
    private readonly PulseDbContext _dbContext;

    public ParticipantRepository(PulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Participant>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Participants
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records.OrderBy(x => x.JoinedAt).ToList();

        return sortedRecords.Select(record => record.ToDomain()).ToList();
    }

    public async Task AddAsync(Participant participant, CancellationToken cancellationToken = default)
    {
        _dbContext.Participants.Add(participant.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
