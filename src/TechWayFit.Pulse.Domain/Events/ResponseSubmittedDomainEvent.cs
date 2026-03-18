namespace TechWayFit.Pulse.Domain.Events;

public sealed record ResponseSubmittedDomainEvent(
    Guid SessionId,
    Guid ActivityId,
    Guid ParticipantId,
    Guid ResponseId,
    DateTimeOffset OccurredAt) : IDomainEvent;
