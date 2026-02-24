using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared ContributionCounterRepository implementation for all providers.
/// </summary>
public class ContributionCounterRepository<TContext> : IContributionCounterRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    public ContributionCounterRepository(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<ContributionCounter?> GetAsync(
        Guid participantId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.ContributionCounters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ParticipantId == participantId && x.SessionId == sessionId, cancellationToken);

        return record?.ToDomain();
    }

    public async Task UpsertAsync(ContributionCounter counter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var existingRecord = await dbContext.ContributionCounters
            .FindAsync(new object[] { counter.ParticipantId }, cancellationToken);

        if (existingRecord != null)
        {
            existingRecord.SessionId = counter.SessionId;
            existingRecord.TotalContributions = counter.TotalContributions;
            existingRecord.UpdatedAt = counter.UpdatedAt;
        }
        else
        {
            dbContext.ContributionCounters.Add(counter.ToRecord());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
