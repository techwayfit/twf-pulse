using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Activities.Abstractions;

/// <summary>
/// Data access façade injected into activity plugins.
/// Hides individual repositories from plugins; each plugin only calls
/// what it needs via this single interface.
/// </summary>
public interface IActivityDataContext
{
    Task<Activity?> GetActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Response>> GetResponsesAsync(Guid activityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Participant>> GetParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
