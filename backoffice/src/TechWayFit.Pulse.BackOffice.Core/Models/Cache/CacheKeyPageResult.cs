namespace TechWayFit.Pulse.BackOffice.Core.Models.Cache;

public sealed record CacheKeyPageResult(
    IReadOnlyList<string> Keys,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);
