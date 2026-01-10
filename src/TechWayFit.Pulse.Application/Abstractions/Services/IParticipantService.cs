using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IParticipantService
{
    Task<Participant> JoinAsync(
        Guid sessionId,
        string? displayName,
        bool isAnonymous,
        IReadOnlyDictionary<string, string?> dimensions,
        DateTimeOffset joinedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Participant>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
