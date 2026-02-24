using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;

namespace TechWayFit.Pulse.BackOffice.Controllers;

/// <summary>
/// Lets SuperAdmin operators inspect and evict entries from the main app's in-process cache
/// by proxying requests through the internal cache management API.
/// </summary>
[Authorize(Policy = PolicyNames.SuperAdminOnly)]
public sealed class CacheController : Controller
{
    private readonly IBackOfficeCacheService _cacheService;

    public CacheController(IBackOfficeCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? pattern, int page = 1)
    {
        const int pageSize = 50;

        var result = string.IsNullOrWhiteSpace(pattern)
            ? await _cacheService.GetAllKeysAsync(page, pageSize)
            : await _cacheService.FindKeysByPatternAsync(pattern, page, pageSize);

        ViewBag.Pattern = pattern;
        ViewBag.Unavailable = result is null;
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evict(string key, string? returnPattern, int returnPage = 1)
    {
        var success = await _cacheService.RemoveKeyAsync(key);
        TempData[success ? "Success" : "Error"] = success
            ? $"Cache key evicted: {key}"
            : $"Failed to evict key: {key}";

        return RedirectToAction(nameof(Index), new { pattern = returnPattern, page = returnPage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvictAll()
    {
        var evicted = await _cacheService.RemoveAllAsync();
        TempData["Success"] = $"Cache cleared. {evicted} key(s) evicted.";
        return RedirectToAction(nameof(Index));
    }
}
