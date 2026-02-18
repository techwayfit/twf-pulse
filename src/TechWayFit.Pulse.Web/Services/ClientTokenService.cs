using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Services;

public interface IClientTokenService
{
    Task<string?> GetFacilitatorTokenAsync(string sessionCode);
    Task StoreFacilitatorTokenAsync(string sessionCode, string token);
}

public class ClientTokenService : IClientTokenService
{
    private readonly IFacilitatorTokenStore _tokenStore;
    private readonly ISessionService _sessionService;
    private readonly IAuthenticationService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ClientTokenService> _logger;

    // Simple in-memory cache for session code to token mapping
    private static readonly Dictionary<string, string> _sessionTokenCache = new();

    public ClientTokenService(
        IFacilitatorTokenStore tokenStore,
        ISessionService sessionService,
        IAuthenticationService authService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ClientTokenService> logger)
    {
        _tokenStore = tokenStore;
        _sessionService = sessionService;
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
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

            // Always validate current user ownership before token reuse/generation
            var session = await _sessionService.GetByCodeAsync(sessionCode);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionCode} not found", sessionCode);
                return null;
            }

            // Verify the current user owns this session
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null, cannot verify facilitator authentication");
                return null;
            }

            var userId = await httpContext.GetFacilitatorUserIdAsync(_authService);
            if (userId == null || session.FacilitatorUserId != userId)
            {
                _logger.LogWarning("User {UserId} is not authorized as facilitator for session {SessionCode} (owner: {OwnerId})", 
                    userId, sessionCode, session.FacilitatorUserId);
                return null;
            }

            // Check cache only after ownership validation
            if (_sessionTokenCache.TryGetValue(sessionCode, out var cachedToken))
            {
                _logger.LogDebug("Found cached facilitator token for authorized user in session {SessionCode}", sessionCode);
                return cachedToken;
            }

            _logger.LogDebug("No cached token found for session {SessionCode}, generating facilitator token", sessionCode);

            // Create and cache the facilitator token
            var auth = _tokenStore.Create(session.Id);
            _sessionTokenCache[sessionCode] = auth.Token;
            
            _logger.LogInformation("Successfully generated facilitator token for session {SessionCode}", sessionCode);
            return auth.Token;
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