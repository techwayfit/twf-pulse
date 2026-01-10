using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface IResponseRepository
{
    Task AddAsync(Response response, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Response>> GetByActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    Task<int> CountByActivityAndParticipantAsync(
        Guid activityId,
        Guid participantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Response>> GetByParticipantAsync(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Response>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
