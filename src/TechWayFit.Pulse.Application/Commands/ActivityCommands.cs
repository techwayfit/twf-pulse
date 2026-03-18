using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Commands;

public sealed record AddActivityCommand(
    Guid SessionId,
    int Order,
    ActivityType Type,
    string Title,
    string? Prompt,
    string? Config,
    int? DurationMinutes = null);

public sealed record UpdateActivityCommand(
    Guid SessionId,
    Guid ActivityId,
    string Title,
    string? Prompt,
    string? Config,
    int? DurationMinutes = null);

public sealed record DeleteActivityCommand(
    Guid SessionId,
    Guid ActivityId);

public sealed record CopyActivityCommand(
    Guid SessionId,
    Guid ActivityId);

public sealed record ReorderActivitiesCommand(
    Guid SessionId,
    IReadOnlyList<Guid> OrderedActivityIds);

public sealed record OpenActivityCommand(
    Guid SessionId,
    Guid ActivityId,
    DateTimeOffset OpenedAt);

public sealed record ReopenActivityCommand(
    Guid SessionId,
    Guid ActivityId,
    DateTimeOffset OpenedAt);

public sealed record CloseActivityCommand(
    Guid SessionId,
    Guid ActivityId,
    DateTimeOffset ClosedAt);
