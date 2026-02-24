using TechWayFit.Pulse.BackOffice.Core.Models.Cache;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// Calls the main Pulse application's internal cache management API to let BackOffice
/// operators inspect and evict cache entries without accessing sensitive cached values.
/// </summary>
public interface IBackOfficeCacheService
{
    /// <summary>Returns a paginated list of all keys currently in the application cache.</summary>
    Task<CacheKeyPageResult?> GetAllKeysAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);

    /// <summary>Returns a paginated list of keys matching the given substring pattern.</summary>
    Task<CacheKeyPageResult?> FindKeysByPatternAsync(string pattern, int page = 1, int pageSize = 50, CancellationToken ct = default);

    /// <summary>Returns whether a specific key currently exists in the cache.</summary>
    Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);

    /// <summary>Evicts a single key from the cache.</summary>
    Task<bool> RemoveKeyAsync(string key, CancellationToken ct = default);

    /// <summary>Evicts all keys from the cache. Returns the number of evicted entries.</summary>
    Task<int> RemoveAllAsync(CancellationToken ct = default);
}
