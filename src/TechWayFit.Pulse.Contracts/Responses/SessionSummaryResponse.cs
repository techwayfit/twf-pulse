using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record SessionSummaryResponse(
    Guid SessionId,
    string Code,
    string Title,
    string? Goal,
    SessionStatus Status,
    Guid? CurrentActivityId,
    DateTimeOffset ExpiresAt);
