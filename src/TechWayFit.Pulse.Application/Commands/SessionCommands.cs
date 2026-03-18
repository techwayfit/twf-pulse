using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Application.Commands;

public sealed record CreateSessionCommand(
    string Code,
    string Title,
    string? Goal,
    string? Context,
    SessionSettings Settings,
    JoinFormSchema JoinFormSchema,
    DateTimeOffset Now,
    Guid? FacilitatorUserId = null,
    Guid? GroupId = null);

public sealed record UpdateSessionCommand(
    Guid SessionId,
    string Title,
    string? Goal,
    string? Context,
    DateTimeOffset Now);

public sealed record SetSessionStatusCommand(
    Guid SessionId,
    SessionStatus Status,
    DateTimeOffset Now);

public sealed record SetCurrentActivityCommand(
    Guid SessionId,
    Guid? ActivityId,
    DateTimeOffset Now);

public sealed record UpdateJoinFormSchemaCommand(
    Guid SessionId,
    JoinFormSchema JoinFormSchema,
    DateTimeOffset Now);

public sealed record SetSessionGroupCommand(
    Guid SessionId,
    Guid? GroupId,
    DateTimeOffset Now);

public sealed record SetSessionScheduleCommand(
    Guid SessionId,
    DateTime? SessionStart,
    DateTime? SessionEnd,
    DateTimeOffset Now);

public sealed record CopySessionCommand(
    Guid SessionId,
    string NewCode,
    DateTimeOffset Now);
