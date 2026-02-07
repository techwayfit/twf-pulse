namespace TechWayFit.Pulse.Contracts.Requests;

public sealed record CreateSessionGroupRequest(
    string Name,
    string? Description,
    int Level,
    Guid? ParentGroupId,
    string? Icon,
    string? Color);

public sealed record UpdateSessionGroupRequest(
    string Name,
    string? Description,
    string? Icon,
    string? Color);

public sealed record AssignSessionToGroupRequest(
    Guid? GroupId);