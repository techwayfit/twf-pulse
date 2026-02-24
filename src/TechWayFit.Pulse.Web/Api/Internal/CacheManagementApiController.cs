using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Api.Internal;

/// <summary>
/// Internal cache management API consumed exclusively by the BackOffice application.
/// All endpoints are protected by <see cref="BackOfficeTokenAuthAttribute"/>.
/// Cache values are never exposed — only keys and metadata are returned.
/// </summary>
[ApiController]
[Route("api/internal/cache")]
[BackOfficeTokenAuth]
public sealed class CacheManagementApiController : ControllerBase
{
    private readonly IApplicationCache _cache;

    public CacheManagementApiController(IApplicationCache cache)
    {
        _cache = cache;
    }

    /// <summary>GET /api/internal/cache/keys — paginated list of all cached keys.</summary>
    [HttpGet("keys")]
    public async Task<IActionResult> GetAllKeys(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)     page     = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var result = await _cache.GetAllKeysAsync(page, pageSize, cancellationToken);
        return Ok(ToResponse(result));
    }

    /// <summary>GET /api/internal/cache/keys/search?pattern=session:id: — paginated filtered keys.</summary>
    [HttpGet("keys/search")]
    public async Task<IActionResult> FindKeys(
        [FromQuery] string pattern,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return BadRequest(new { error = "pattern is required." });

        if (page < 1)     page     = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var result = await _cache.FindKeysByPatternAsync(pattern, page, pageSize, cancellationToken);
        return Ok(ToResponse(result));
    }

    /// <summary>GET /api/internal/cache/keys/{key}/exists — check whether a key is cached.</summary>
    [HttpGet("keys/{key}/exists")]
    public async Task<IActionResult> KeyExists(
        string key,
        CancellationToken cancellationToken = default)
    {
        // A key is "alive" if it is still in the registry (registry is kept in sync with eviction).
        var page = await _cache.FindKeysByPatternAsync(key, 1, 1, cancellationToken);
        var exists = page.Keys.Contains(key, StringComparer.Ordinal);
        return Ok(new { key, exists });
    }

    /// <summary>DELETE /api/internal/cache/keys/{key} — evict a single key.</summary>
    [HttpDelete("keys/{key}")]
    public async Task<IActionResult> RemoveKey(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
        return NoContent();
    }

    /// <summary>DELETE /api/internal/cache — evict all keys.</summary>
    [HttpDelete]
    public async Task<IActionResult> RemoveAll(CancellationToken cancellationToken = default)
    {
        var count = await _cache.GetKeyCountAsync(cancellationToken);
        await _cache.RemoveAllAsync(cancellationToken);
        return Ok(new { evicted = count });
    }

    // -------------------------------------------------------------------------
    // Mapping helper — CacheKeysPage → anonymous response (no internal types leaked)
    // -------------------------------------------------------------------------

    private static object ToResponse(CacheKeysPage page) => new
    {
        keys        = page.Keys,
        totalCount  = page.TotalCount,
        page        = page.Page,
        pageSize    = page.PageSize,
        totalPages  = page.TotalPages,
        hasPrevious = page.HasPrevious,
        hasNext     = page.HasNext
    };
}
