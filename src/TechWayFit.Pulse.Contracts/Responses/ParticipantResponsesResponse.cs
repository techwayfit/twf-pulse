namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record ParticipantResponsesResponse(
    Guid ParticipantId,
    IReadOnlyList<ParticipantResponseItem> Responses);
