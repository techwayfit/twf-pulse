namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record SessionGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    int Level,
    Guid? ParentGroupId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SessionGroupHierarchyResponse(
    Guid Id,
    string Name,
    string? Description,
    int Level,
    Guid? ParentGroupId,
    IReadOnlyCollection<SessionGroupHierarchyResponse> Children,
    int SessionCount);