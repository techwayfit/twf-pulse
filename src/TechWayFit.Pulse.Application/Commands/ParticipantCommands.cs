namespace TechWayFit.Pulse.Application.Commands;

public sealed record JoinParticipantCommand(
    Guid SessionId,
    string? DisplayName,
    bool IsAnonymous,
    IReadOnlyDictionary<string, string?> Dimensions,
    DateTimeOffset JoinedAt);
