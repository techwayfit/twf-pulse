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
