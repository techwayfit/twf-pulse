using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Infrastructure.Caching;

/// <summary>
/// In-process <see cref="IApplicationCache"/> backed by <see cref="IMemoryCache"/>.
/// Suitable for single-node deployments. Swap for a distributed implementation (e.g. Redis)
/// by registering a different <see cref="IApplicationCache"/> in DI without touching any consumer.
/// </summary>
public sealed class MemoryApplicationCache : IApplicationCache
{
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Shared key registry. A static ConcurrentDictionary is used so the set of live
    /// keys is available for enumeration via <see cref="GetAllKeysAsync"/> and
    /// <see cref="FindKeysByPatternAsync"/>. The <see cref="byte"/> value is unused
    /// (the dictionary is used purely as a thread-safe set).
    /// IMemoryCache does not expose its keys natively, so we maintain this registry
    /// ourselves. Entries are removed on explicit eviction or via the post-eviction callback.
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> _keyRegistry = new(StringComparer.Ordinal);

    public MemoryApplicationCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry,
            // Evict under memory pressure before expiry
            Size = 1
        };

        // Keep key registry in sync when IMemoryCache evicts the entry on its own
        // (TTL expiry, memory pressure) so GetAllKeysAsync never returns stale keys.
        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            if (evictedKey is string k)
            {
                _keyRegistry.TryRemove(k, out _);
            }
        });

        _keyRegistry.TryAdd(key, 0);
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _keyRegistry.TryRemove(key, out _);
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveAllAsync(CancellationToken cancellationToken = default)
    {
        var keys = _keyRegistry.Keys.ToArray();
        foreach (var key in keys)
        {
            _keyRegistry.TryRemove(key, out _);
            _cache.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task<int> GetKeyCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_keyRegistry.Count);

    public Task<CacheKeysPage> GetAllKeysAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var allKeys = _keyRegistry.Keys.OrderBy(k => k).ToArray();
        return Task.FromResult(BuildPage(allKeys, page, pageSize));
    }

    public Task<CacheKeysPage> FindKeysByPatternAsync(
        string pattern,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var matched = _keyRegistry.Keys
            .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k)
            .ToArray();

        return Task.FromResult(BuildPage(matched, page, pageSize));
    }

    private static CacheKeysPage BuildPage(string[] allKeys, int page, int pageSize)
    {
        var total = allKeys.Length;
        var paged = allKeys
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new CacheKeysPage(paged, total, page, pageSize);
    }
}
