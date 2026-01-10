using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface IContributionCounterRepository
{
    Task<ContributionCounter?> GetAsync(Guid participantId, Guid sessionId, CancellationToken cancellationToken = default);

    Task UpsertAsync(ContributionCounter counter, CancellationToken cancellationToken = default);
}
