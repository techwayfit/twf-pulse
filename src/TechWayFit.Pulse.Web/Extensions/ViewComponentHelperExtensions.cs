using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TechWayFit.Pulse.Web.Extensions;

/// <summary>
/// Extension methods for caching ViewComponent output.
/// Provides a clean API: Component.InvokeCachedAsync() that caches rendered HTML.
/// </summary>
public static class ViewComponentHelperExtensions
{
    /// <summary>
    /// Invokes a ViewComponent and caches the rendered HTML output.
    /// Subsequent calls with the same component name and arguments return cached HTML
    /// without executing the ViewComponent logic.
    /// </summary>
    /// <param name="component">The ViewComponent helper</param>
    /// <param name="componentName">Name of the ViewComponent</param>
    /// <param name="arguments">Arguments to pass to the ViewComponent</param>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="cacheKey">Optional custom cache key. If not provided, auto-generated from component name and arguments.</param>
    /// <param name="cacheDuration">Cache duration. Default is 5 minutes.</param>
    /// <returns>Cached or freshly rendered HTML content</returns>
    public static async Task<IHtmlContent> InvokeCachedAsync(
     this IViewComponentHelper component,
    string componentName,
        object? arguments,
        IMemoryCache cache,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
    {
        // Generate cache key if not provided
        var key = cacheKey ?? GenerateCacheKey(componentName, arguments);
        var duration = cacheDuration ?? TimeSpan.FromMinutes(5);

        // Try to get from cache
        if (cache.TryGetValue(key, out string? cachedHtml) && cachedHtml != null)
        {
            return new HtmlString(cachedHtml);
        }

        // Cache miss - invoke component and cache result
        var result = await component.InvokeAsync(componentName, arguments);

        // Convert IHtmlContent to string for caching
        var htmlString = await RenderHtmlContentAsync(result);

        // Cache the HTML string
        cache.Set(key, htmlString, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration,
            Size = htmlString.Length // For size-based eviction
        });

        return new HtmlString(htmlString);
    }

    /// <summary>
    /// Invokes a ViewComponent with typed arguments and caches the output.
    /// </summary>
    public static Task<IHtmlContent> InvokeCachedAsync<T>(
        this IViewComponentHelper component,
     string componentName,
        T arguments,
        IMemoryCache cache,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
    {
        return InvokeCachedAsync(component, componentName, (object?)arguments, cache, cacheKey, cacheDuration);
    }

    /// <summary>
    /// Generates a deterministic cache key from component name and arguments.
    /// Uses SHA256 hash of JSON-serialized arguments for consistency.
    /// </summary>
    private static string GenerateCacheKey(string componentName, object? arguments)
    {
        if (arguments == null)
        {
            return $"vc:{componentName}:noargs";
        }

        try
        {
            // Serialize arguments to JSON for consistent hashing
            var json = JsonSerializer.Serialize(arguments, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Generate SHA256 hash for compact, deterministic key
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            var hashString = Convert.ToHexString(hash)[..16]; // Take first 16 chars

            return $"vc:{componentName}:{hashString}";
        }
        catch
        {
            // Fallback to simple string representation if serialization fails
            return $"vc:{componentName}:{arguments.GetHashCode():X}";
        }
    }

    /// <summary>
    /// Renders IHtmlContent to a string for caching.
    /// </summary>
    private static async Task<string> RenderHtmlContentAsync(IHtmlContent htmlContent)
    {
        await using var writer = new StringWriter();
        htmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        await writer.FlushAsync();
        return writer.ToString();
    }

    /// <summary>
    /// Invalidates cached ViewComponent output by cache key.
    /// Useful when underlying data changes and cache needs to be refreshed.
    /// </summary>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="componentName">Name of the ViewComponent</param>
    /// <param name="arguments">Arguments used when caching</param>
    public static void InvalidateComponentCache(
        this IMemoryCache cache,
     string componentName,
        object? arguments = null)
    {
        var key = GenerateCacheKey(componentName, arguments);
        cache.Remove(key);
    }

    /// <summary>
    /// Invalidates cached ViewComponent output by explicit cache key.
    /// </summary>
    public static void InvalidateComponentCache(
        this IMemoryCache cache,
        string cacheKey)
    {
        cache.Remove(cacheKey);
    }
}
