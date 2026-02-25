using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Caching decorator for <see cref="IActivityRepository"/>.
/// Wraps any concrete repository implementation and reduces DB round-trips for hot read paths.
///
/// Cache invalidation strategy:
/// - Single-entity keys (by-id) are evicted on every write.
/// - List keys (by-session) are evicted on every write because activity state transitions
///   (Open/Close/Reorder) must be visible immediately to participants and facilitators.
/// </summary>
public sealed class CachingActivityRepository : IActivityRepository
{
    private readonly IActivityRepository _inner;
    private readonly IApplicationCache _cache;

    // Hot-path reads (used on every response submission / participant page load)
    private static readonly TimeSpan SingleEntityTtl = TimeSpan.FromMinutes(5);

    // List reads (agenda / facilitator dashboard) — short TTL as a safety net,
    // but writes always evict explicitly so staleness is minimal.
    private static readonly TimeSpan ListTtl = TimeSpan.FromMinutes(2);

    public CachingActivityRepository(IActivityRepository inner, IApplicationCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    // -------------------------------------------------------------------------
    // Read operations — cache-aside
    // -------------------------------------------------------------------------

    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.ById(id);
        var cached = await _cache.GetAsync<Activity>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var activity = await _inner.GetByIdAsync(id, cancellationToken);
        if (activity is not null)
        {
            await _cache.SetAsync(key, activity, SingleEntityTtl, cancellationToken);
        }

        return activity;
    }

    public async Task<IReadOnlyList<Activity>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.BySession(sessionId);
        var cached = await _cache.GetAsync<CachedList<Activity>>(key, cancellationToken);
        if (cached is not null)
        {
            return cached.Items;
        }

        var activities = await _inner.GetBySessionAsync(sessionId, cancellationToken);
        await _cache.SetAsync(key, new CachedList<Activity>(activities), ListTtl, cancellationToken);
        return activities;
    }

    // -------------------------------------------------------------------------
    // Write operations — delegate then invalidate affected keys
    // -------------------------------------------------------------------------

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await _inner.AddAsync(activity, cancellationToken);

        // Evict the session's activity list so the new activity is visible immediately.
        await _cache.RemoveAsync(CacheKeys.BySession(activity.SessionId), cancellationToken);
    }

    public async Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateAsync(activity, cancellationToken);

        // Evict both the single-entity cache and the session list.
        // Activity state transitions (Open/Close/Reorder) must be visible immediately.
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.ById(activity.Id), cancellationToken),
            _cache.RemoveAsync(CacheKeys.BySession(activity.SessionId), cancellationToken)
        );
    }

    public async Task DeleteAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(activity, cancellationToken);

        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.ById(activity.Id), cancellationToken),
            _cache.RemoveAsync(CacheKeys.BySession(activity.SessionId), cancellationToken)
        );
    }

    // -------------------------------------------------------------------------
    // Cache key helpers
    // -------------------------------------------------------------------------

    private static class CacheKeys
    {
        private static string N(Guid id) => id.ToString("N").ToLowerInvariant();

        public static string ById(Guid id)           => $"activity:id:{N(id)}";
        public static string BySession(Guid session) => $"activity:session:{N(session)}";
    }

    // -------------------------------------------------------------------------
    // Small wrapper type so IReadOnlyList<T> can be stored via the
    // IApplicationCache generic constraint (class).
    // -------------------------------------------------------------------------

    private sealed class CachedList<T>(IReadOnlyList<T> items)
    {
        public IReadOnlyList<T> Items { get; } = items;
    }
}
