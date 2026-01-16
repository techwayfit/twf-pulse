using Microsoft.AspNetCore.Components;

namespace TechWayFit.Pulse.Web.Services;

public interface IClientTokenService
{
    Task<string?> GetFacilitatorTokenAsync();
}

public class ClientTokenService : IClientTokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ClientTokenService> _logger;

    public ClientTokenService(IHttpContextAccessor httpContextAccessor, ILogger<ClientTokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string?> GetFacilitatorTokenAsync()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("FacilitatorToken", out var tokenObj) == true)
            {
                return tokenObj as string;
            }

            _logger.LogWarning("No facilitator token found in request context");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve facilitator token");
            return null;
        }
    }
}