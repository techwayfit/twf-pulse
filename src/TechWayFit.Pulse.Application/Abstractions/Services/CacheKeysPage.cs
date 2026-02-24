namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// A page of cache keys returned by <see cref="IApplicationCache.GetAllKeysAsync"/> or
/// <see cref="IApplicationCache.FindKeysByPatternAsync"/>.
/// Values are intentionally excluded — keys are safe to surface in management UIs,
/// but cache values may contain sensitive domain data.
/// </summary>
public sealed record CacheKeysPage(
    IReadOnlyList<string> Keys,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages    => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPrevious  => Page > 1;
    public bool HasNext      => Page < TotalPages;
}
