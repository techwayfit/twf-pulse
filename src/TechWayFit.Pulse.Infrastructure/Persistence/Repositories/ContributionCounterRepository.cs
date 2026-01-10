using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

public sealed class ContributionCounterRepository : IContributionCounterRepository
{
    private readonly PulseDbContext _dbContext;

    public ContributionCounterRepository(PulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContributionCounter?> GetAsync(
        Guid participantId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ContributionCounters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ParticipantId == participantId && x.SessionId == sessionId, cancellationToken);

        return record?.ToDomain();
    }

    public async Task UpsertAsync(ContributionCounter counter, CancellationToken cancellationToken = default)
    {
        var record = counter.ToRecord();
        var exists = await _dbContext.ContributionCounters
            .AsNoTracking()
            .AnyAsync(x => x.ParticipantId == record.ParticipantId && x.SessionId == record.SessionId, cancellationToken);

        if (exists)
        {
            _dbContext.ContributionCounters.Update(record);
        }
        else
        {
            _dbContext.ContributionCounters.Add(record);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
