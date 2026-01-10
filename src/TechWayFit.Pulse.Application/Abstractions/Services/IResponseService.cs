using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IResponseService
{
    Task<Response> SubmitAsync(
        Guid sessionId,
        Guid activityId,
        Guid participantId,
        string payload,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Response>> GetByActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Response>> GetByParticipantAsync(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default);
}
