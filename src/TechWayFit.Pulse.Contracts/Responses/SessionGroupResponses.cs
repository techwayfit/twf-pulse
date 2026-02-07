namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record SessionGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    int Level,
    Guid? ParentGroupId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Icon,
    string? Color);

public sealed record SessionGroupHierarchyResponse(
    Guid Id,
    string Name,
    string? Description,
    int Level,
    Guid? ParentGroupId,
    IReadOnlyCollection<SessionGroupHierarchyResponse> Children,
    int SessionCount,
    string? Icon,
    string? Color);