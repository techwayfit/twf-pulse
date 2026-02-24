namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Generic cache abstraction. Implementations can be backed by IMemoryCache,
/// IDistributedCache (Redis, etc.) or any other store.
/// </summary>
public interface IApplicationCache
{
    /// <summary>Returns the cached value for <paramref name="key"/>, or <c>null</c> if not present.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/> for <paramref name="expiry"/>.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Removes the entry for <paramref name="key"/> if it exists.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes all tracked entries from the cache.</summary>
    Task RemoveAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the total number of keys currently tracked in the cache.</summary>
    Task<int> GetKeyCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of all keys currently tracked in the cache.
    /// Values are never included — consumers should use <see cref="GetAsync{T}"/> if needed.
    /// </summary>
    Task<CacheKeysPage> GetAllKeysAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of keys whose name contains <paramref name="pattern"/> (case-insensitive substring match).
    /// Pass a key prefix, suffix, or any substring to narrow results.
    /// </summary>
    Task<CacheKeysPage> FindKeysByPatternAsync(string pattern, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
