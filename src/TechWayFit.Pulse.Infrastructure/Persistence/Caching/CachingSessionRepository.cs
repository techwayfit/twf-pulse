using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Caching decorator for <see cref="ISessionRepository"/>.
/// Wraps any concrete repository implementation and reduces DB round-trips for hot read paths
/// (session-by-id, session-by-code) that are called on every response submission, activity
/// transition and participant page load.
///
/// Cache invalidation strategy:
/// - Single-entity keys (by-id / by-code) are evicted on every write.
/// - List keys (by-facilitator / by-group) use a short TTL and are not explicitly invalidated,
///   because they are only read from facilitator dashboards and accept brief eventual consistency.
/// </summary>
public sealed class CachingSessionRepository : ISessionRepository
{
    private readonly ISessionRepository _inner;
    private readonly IApplicationCache _cache;

    // Hot-path reads (used in every response submission / participant join)
    private static readonly TimeSpan SingleEntityTtl = TimeSpan.FromMinutes(5);

    // List reads (facilitator dashboard pages — tolerate short staleness)
    private static readonly TimeSpan ListTtl = TimeSpan.FromMinutes(2);

    public CachingSessionRepository(ISessionRepository inner, IApplicationCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    // -------------------------------------------------------------------------
    // Read operations — cache-aside
    // -------------------------------------------------------------------------

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.ById(id);
        var cached = await _cache.GetAsync<Session>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var session = await _inner.GetByIdAsync(id, cancellationToken);
        if (session is not null)
        {
            await _cache.SetAsync(key, session, SingleEntityTtl, cancellationToken);
        }

        return session;
    }

    public async Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.ByCode(code);
        var cached = await _cache.GetAsync<Session>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var session = await _inner.GetByCodeAsync(code, cancellationToken);
        if (session is not null)
        {
            await _cache.SetAsync(key, session, SingleEntityTtl, cancellationToken);
        }

        return session;
    }

    public async Task<IReadOnlyList<Session>> GetByFacilitatorUserIdAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.ByFacilitator(facilitatorUserId);
        var cached = await _cache.GetAsync<CachedList<Session>>(key, cancellationToken);
        if (cached is not null)
        {
            return cached.Items;
        }

        var sessions = await _inner.GetByFacilitatorUserIdAsync(facilitatorUserId, cancellationToken);
        await _cache.SetAsync(key, new CachedList<Session>(sessions), ListTtl, cancellationToken);
        return sessions;
    }

    public async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
        Guid facilitatorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.ByFacilitatorPaged(facilitatorUserId, page, pageSize);
        var cached = await _cache.GetAsync<CachedPage<Session>>(key, cancellationToken);
        if (cached is not null)
        {
            return (cached.Items, cached.TotalCount);
        }

        var (sessions, totalCount) = await _inner.GetByFacilitatorUserIdPaginatedAsync(
            facilitatorUserId, page, pageSize, cancellationToken);

        await _cache.SetAsync(key, new CachedPage<Session>(sessions, totalCount), ListTtl, cancellationToken);
        return (sessions, totalCount);
    }

    public async Task<IReadOnlyCollection<Session>> GetByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.ByGroup(groupId, facilitatorUserId);
        var cached = await _cache.GetAsync<CachedList<Session>>(key, cancellationToken);
        if (cached is not null)
        {
            return cached.Items;
        }

        var sessions = await _inner.GetByGroupAsync(groupId, facilitatorUserId, cancellationToken);
        await _cache.SetAsync(key, new CachedList<Session>((IReadOnlyList<Session>)sessions), ListTtl, cancellationToken);
        return sessions;
    }

    // -------------------------------------------------------------------------
    // Write operations — delegate then invalidate single-entity keys
    // -------------------------------------------------------------------------

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        await _inner.AddAsync(session, cancellationToken);

        // Evict facilitator list caches so the new session appears immediately
        // on the facilitator's dashboard without waiting for TTL expiry.
        if (session.FacilitatorUserId.HasValue)
        {
            var userId = session.FacilitatorUserId.Value;
            var keysToEvict = await _cache.FindKeysByPatternAsync(
                CacheKeys.ByFacilitator(userId), cancellationToken: cancellationToken);

            await Task.WhenAll(keysToEvict.Keys.Select(k => _cache.RemoveAsync(k, cancellationToken)));
        }
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateAsync(session, cancellationToken);

        // Evict stale single-entity entries so next read reflects the updated status,
        // settings, or currentActivityId immediately.
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.ById(session.Id), cancellationToken),
            _cache.RemoveAsync(CacheKeys.ByCode(session.Code), cancellationToken)
        );
    }

    // -------------------------------------------------------------------------
    // Cache key helpers
    // -------------------------------------------------------------------------

    private static class CacheKeys
    {
        // GUIDs are normalised to lowercase N-format (no hyphens or braces).
        // ToLowerInvariant() is called explicitly — "N" is lowercase in current .NET
        // runtimes but is not formally guaranteed, so we enforce it here.
        private static string N(Guid id) => id.ToString("N").ToLowerInvariant();
        private static string N(Guid? id) => id?.ToString("N").ToLowerInvariant() ?? "null";

        public static string ById(Guid id) => $"session:id:{N(id)}";
        public static string ByCode(string code) => $"session:code:{code}";
        public static string ByFacilitator(Guid userId) => $"session:facilitator:{N(userId)}";
        public static string ByFacilitatorPaged(Guid userId, int page, int size) => $"session:facilitator:{N(userId)}:p:{page}:{size}";
        public static string ByGroup(Guid? groupId, Guid userId) => $"session:group:{N(groupId)}:{N(userId)}";
    }

    // -------------------------------------------------------------------------
    // Small wrapper types so IReadOnlyList/IReadOnlyCollection can be stored
    // via the IApplicationCache generic constraint (class).
    // -------------------------------------------------------------------------

    private sealed class CachedList<T>(IReadOnlyList<T> items)
    {
        public IReadOnlyList<T> Items { get; } = items;
    }

    private sealed class CachedPage<T>(IReadOnlyList<T> items, int totalCount)
    {
        public IReadOnlyList<T> Items { get; } = items;
        public int TotalCount { get; } = totalCount;
    }
}
