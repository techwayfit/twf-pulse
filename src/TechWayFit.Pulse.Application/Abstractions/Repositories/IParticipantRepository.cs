using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface IParticipantRepository
{
    Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Participant>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task AddAsync(Participant participant, CancellationToken cancellationToken = default);
}
