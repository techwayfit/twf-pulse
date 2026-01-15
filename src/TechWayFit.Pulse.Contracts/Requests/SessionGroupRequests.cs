namespace TechWayFit.Pulse.Contracts.Requests;

public sealed record CreateSessionGroupRequest(
    string Name,
    string? Description,
    int Level,
    Guid? ParentGroupId);

public sealed record UpdateSessionGroupRequest(
    string Name,
    string? Description);

public sealed record AssignSessionToGroupRequest(
    Guid? GroupId);