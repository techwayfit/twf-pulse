namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record ParticipantResponseItem(
    Guid ResponseId,
    Guid ActivityId,
    string Payload,
    DateTimeOffset CreatedAt);
