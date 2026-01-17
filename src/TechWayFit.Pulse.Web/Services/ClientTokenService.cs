using Microsoft.AspNetCore.Components;
using TechWayFit.Pulse.Web.Api;

namespace TechWayFit.Pulse.Web.Services;

public interface IClientTokenService
{
    Task<string?> GetFacilitatorTokenAsync(string sessionCode);
    Task StoreFacilitatorTokenAsync(string sessionCode, string token);
}

public class ClientTokenService : IClientTokenService
{
    private readonly IFacilitatorTokenStore _tokenStore;
    private readonly IPulseApiService _apiService;
    private readonly ILogger<ClientTokenService> _logger;

    // Simple in-memory cache for session code to token mapping
    private static readonly Dictionary<string, string> _sessionTokenCache = new();

    public ClientTokenService(
        IFacilitatorTokenStore tokenStore, 
        IPulseApiService apiService, 
        ILogger<ClientTokenService> logger)
    {
        _tokenStore = tokenStore;
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<string?> GetFacilitatorTokenAsync(string sessionCode)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionCode))
            {
                throw new ArgumentException("Session code is required", nameof(sessionCode));
            }

            // Check our simple cache first
            if (_sessionTokenCache.TryGetValue(sessionCode, out var cachedToken))
            {
                _logger.LogDebug("Found cached facilitator token for session {SessionCode}", sessionCode);
                return cachedToken;
            }

            // If not in cache, try to join as facilitator to get a token
            _logger.LogDebug("No cached token found for session {SessionCode}, attempting to join as facilitator", sessionCode);
            
            var joinRequest = new TechWayFit.Pulse.Contracts.Requests.JoinFacilitatorRequest
            {
                DisplayName = "Facilitator"
            };
            
            var joinResponse = await _apiService.JoinAsFacilitatorAsync(sessionCode, joinRequest);
            
            if (joinResponse?.Token != null)
            {
                // Cache the token for future use
                _sessionTokenCache[sessionCode] = joinResponse.Token;
                _logger.LogInformation("Successfully joined as facilitator for session {SessionCode}", sessionCode);
                return joinResponse.Token;
            }

            _logger.LogWarning("Failed to join as facilitator for session {SessionCode}", sessionCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve facilitator token for session {SessionCode}", sessionCode);
            return null;
        }
    }

    public async Task StoreFacilitatorTokenAsync(string sessionCode, string token)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionCode))
            {
                throw new ArgumentException("Session code is required", nameof(sessionCode));
            }
            
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token is required", nameof(token));
            }

            // Simply cache the token for future use
            _sessionTokenCache[sessionCode] = token;
            _logger.LogDebug("Cached facilitator token for session {SessionCode}", sessionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store facilitator token for session {SessionCode}", sessionCode);
            throw;
        }
    }
}