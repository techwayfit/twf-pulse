namespace TechWayFit.Pulse.Application.Commands;

public sealed record SubmitResponseCommand(
    Guid SessionId,
    Guid ActivityId,
    Guid ParticipantId,
    string Payload,
    DateTimeOffset CreatedAt);
