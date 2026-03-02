namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record BulkCreateActivitiesResponse(
    int SuccessCount,
    IReadOnlyList<Guid> CreatedActivityIds,
    IReadOnlyList<string>? Errors = null
);
