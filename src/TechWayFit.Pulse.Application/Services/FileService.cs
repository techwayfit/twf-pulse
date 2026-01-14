using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Application.Services;

/// <summary>
/// File service implementation with in-memory caching to reduce disk I/O.
/// </summary>
public sealed class FileService : IFileService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FileService> _logger;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(1);

    public FileService(
        IMemoryCache cache,
        ILogger<FileService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        var cacheKey = $"file_content_{filePath}";

        // Try to get from cache first
        if (_cache.TryGetValue<string>(cacheKey, out var cachedContent))
        {
            _logger.LogDebug("File content retrieved from cache: {FilePath}", filePath);
            return cachedContent!;
        }

        // File not in cache, read from disk
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);

            // Cache the content
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, content, cacheOptions);

            _logger.LogInformation("File read and cached successfully: {FilePath}", filePath);

            return content;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            throw new IOException($"Error reading file: {filePath}", ex);
        }
    }

    public void InvalidateCache(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var cacheKey = $"file_content_{filePath}";
        _cache.Remove(cacheKey);

        _logger.LogInformation("Cache invalidated for file: {FilePath}", filePath);
    }

    public void ClearCache()
    {
        // Note: IMemoryCache doesn't have a built-in Clear method
        // If needed, you could maintain a list of cached keys or use a different caching strategy
        _logger.LogWarning("ClearCache called - IMemoryCache doesn't support clearing all entries. Consider disposing and recreating cache if needed.");
    }
}
