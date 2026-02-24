using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Cache;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

/// <summary>
/// Calls the main Pulse application's <c>/api/internal/cache</c> endpoints.
/// Requires <c>MainApp:BaseUrl</c> and <c>MainApp:BackOfficeApiToken</c> in configuration.
/// </summary>
public sealed class BackOfficeCacheService : IBackOfficeCacheService
{
    public const string HttpClientName = "MainAppCacheApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration     _configuration;
    private readonly ILogger<BackOfficeCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public BackOfficeCacheService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BackOfficeCacheService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration     = configuration;
        _logger            = logger;
    }

    public async Task<CacheKeyPageResult?> GetAllKeysAsync(
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync(
                $"api/internal/cache/keys?page={page}&pageSize={pageSize}", ct);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CacheKeyPageResult>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all cache keys from main app");
            return null;
        }
    }

    public async Task<CacheKeyPageResult?> FindKeysByPatternAsync(
        string pattern, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync(
                $"api/internal/cache/keys/search?pattern={Uri.EscapeDataString(pattern)}&page={page}&pageSize={pageSize}", ct);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CacheKeyPageResult>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search cache keys with pattern '{Pattern}'", pattern);
            return null;
        }
    }

    public async Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var client   = CreateClient();
            var response = await client.GetAsync(
                $"api/internal/cache/keys/{Uri.EscapeDataString(key)}/exists", ct);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return json.GetProperty("exists").GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of cache key '{Key}'", key);
            return false;
        }
    }

    public async Task<bool> RemoveKeyAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var client   = CreateClient();
            var response = await client.DeleteAsync(
                $"api/internal/cache/keys/{Uri.EscapeDataString(key)}", ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evict cache key '{Key}'", key);
            return false;
        }
    }

    public async Task<int> RemoveAllAsync(CancellationToken ct = default)
    {
        try
        {
            var client   = CreateClient();
            var response = await client.DeleteAsync("api/internal/cache", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return json.GetProperty("evicted").GetInt32();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evict all cache keys");
            return 0;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        // Attach token on every call so the client can be a transient factory client
        var token = _configuration["MainApp:BackOfficeApiToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Remove("X-BackOffice-Token");
            client.DefaultRequestHeaders.Add("X-BackOffice-Token", token);
        }

        return client;
    }
}
