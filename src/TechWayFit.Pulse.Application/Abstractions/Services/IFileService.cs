namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Service for reading files from the file system with caching support.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Reads file content from the specified path.
    /// Results are cached to minimize disk I/O operations.
    /// </summary>
    /// <param name="filePath">Relative or absolute file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content as string</returns>
    Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache for a specific file.
    /// </summary>
    /// <param name="filePath">File path to invalidate</param>
    void InvalidateCache(string filePath);

    /// <summary>
    /// Clears all cached file content.
    /// </summary>
    void ClearCache();
}
