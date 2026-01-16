using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace TechWayFit.Pulse.Web.Services;

public interface IFacilitatorTokenService
{
    Task<string> GetOrCreateTokenAsync(Guid facilitatorUserId);
    Task<string?> GetTokenAsync(Guid facilitatorUserId);
    Task<Guid?> ValidateTokenAsync(string token);
    Task RevokeTokenAsync(Guid facilitatorUserId);
}

public class FacilitatorTokenService : IFacilitatorTokenService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FacilitatorTokenService> _logger;
    private readonly TimeSpan _tokenExpiry = TimeSpan.FromHours(6); // 6 hour expiry

    public FacilitatorTokenService(IMemoryCache cache, ILogger<FacilitatorTokenService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetOrCreateTokenAsync(Guid facilitatorUserId)
    {
        var cacheKey = $"facilitator_token_{facilitatorUserId}";
        
        if (_cache.TryGetValue(cacheKey, out string? existingToken) && !string.IsNullOrEmpty(existingToken))
        {
            return existingToken;
        }

        // Generate new token
        var token = GenerateSecureToken();
        var expiration = DateTimeOffset.UtcNow.Add(_tokenExpiry);

        // Store both directions for efficient lookup
        _cache.Set(cacheKey, token, expiration);
        _cache.Set($"token_user_{token}", facilitatorUserId, expiration);

        _logger.LogInformation("Generated new facilitator token for user {UserId}", facilitatorUserId);
        
        return token;
    }

    public async Task<string?> GetTokenAsync(Guid facilitatorUserId)
    {
        var cacheKey = $"facilitator_token_{facilitatorUserId}";
        _cache.TryGetValue(cacheKey, out string? token);
        return token;
    }

    public async Task<Guid?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var cacheKey = $"token_user_{token}";
        if (_cache.TryGetValue(cacheKey, out Guid userId))
        {
            return userId;
        }

        return null;
    }

    public async Task RevokeTokenAsync(Guid facilitatorUserId)
    {
        var cacheKey = $"facilitator_token_{facilitatorUserId}";
        
        if (_cache.TryGetValue(cacheKey, out string? token) && !string.IsNullOrEmpty(token))
        {
            _cache.Remove(cacheKey);
            _cache.Remove($"token_user_{token}");
            _logger.LogInformation("Revoked facilitator token for user {UserId}", facilitatorUserId);
        }
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}