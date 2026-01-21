using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record AgendaActivityResponse(
    Guid ActivityId,
    int Order,
    ActivityType Type,
    string Title,
    string? Prompt,
    string? Config,
    ActivityStatus Status,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? ClosedAt,
    int? DurationMinutes);
