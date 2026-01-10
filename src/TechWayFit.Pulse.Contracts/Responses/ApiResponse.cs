namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record ApiResponse<T>(T? Data, IReadOnlyList<ApiError>? Errors = null, object? Meta = null);
