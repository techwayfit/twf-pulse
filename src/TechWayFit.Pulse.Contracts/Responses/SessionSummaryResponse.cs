using TechWayFit.Pulse.Contracts.Enums;
using TechWayFit.Pulse.Contracts.Models;

namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record SessionSummaryResponse(
    Guid SessionId,
    string Code,
    string Title,
    string? Goal,
    SessionStatus Status,
    Guid? CurrentActivityId,
    DateTimeOffset ExpiresAt,
    Guid? GroupId = null,
    List<JoinFormFieldDto>? JoinFormFields = null);
