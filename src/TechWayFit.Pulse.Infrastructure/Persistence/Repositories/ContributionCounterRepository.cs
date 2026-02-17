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
        // Try to find the existing tracked entity using the primary key
        var existingRecord = await _dbContext.ContributionCounters
            .FindAsync(new object[] { counter.ParticipantId }, cancellationToken);

        if (existingRecord != null)
        {
            // Update the existing tracked entity's properties
            existingRecord.SessionId = counter.SessionId;
            existingRecord.TotalContributions = counter.TotalContributions;
            existingRecord.UpdatedAt = counter.UpdatedAt;
        }
        else
        {
            // Add a new record
            _dbContext.ContributionCounters.Add(counter.ToRecord());
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
