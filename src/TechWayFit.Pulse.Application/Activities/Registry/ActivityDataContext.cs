using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Activities.Registry;

/// <summary>
/// Thin façade over the individual repositories, injected into activity plugins
/// via <see cref="IActivityDataContext"/>.
/// <para>
/// This decouples plugins from the concrete repository interfaces and ensures that
/// fetching responses, participants, or the activity record is a single method call
/// — not three separate constructor parameters per plugin.
/// </para>
/// </summary>
public sealed class ActivityDataContext : IActivityDataContext
{
    private readonly IActivityRepository _activities;
    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;

    public ActivityDataContext(
        IActivityRepository activities,
        IResponseRepository responses,
        IParticipantRepository participants)
    {
        _activities   = activities;
        _responses    = responses;
        _participants = participants;
    }

    /// <inheritdoc />
    public Task<Activity?> GetActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
        => _activities.GetByIdAsync(activityId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Response>> GetResponsesAsync(Guid activityId, CancellationToken cancellationToken = default)
        => _responses.GetByActivityAsync(activityId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Participant>> GetParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => _participants.GetBySessionAsync(sessionId, cancellationToken);
}
