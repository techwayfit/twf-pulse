using TechWayFit.Pulse.Web.Services;
using TechWayFit.Pulse.Web.Extensions;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Middleware;

public class FacilitatorTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFacilitatorTokenService _tokenService;
    private readonly ILogger<FacilitatorTokenMiddleware> _logger;

    public FacilitatorTokenMiddleware(
        RequestDelegate next,
        IFacilitatorTokenService tokenService,
        ILogger<FacilitatorTokenMiddleware> logger)
    {
        _next = next;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process authenticated facilitator requests
        if (context.User?.Identity?.IsAuthenticated == true && 
            context.IsFacilitatorAuthenticated())
        {
            try
            {
                // Get scoped authentication service from request services
                var authService = context.RequestServices.GetRequiredService<IAuthenticationService>();
                
                // Get or create facilitator token for this user
                var userId = await context.GetFacilitatorUserIdAsync(authService);
                if (userId.HasValue)
                {
                    var token = await _tokenService.GetOrCreateTokenAsync(userId.Value);
                    
                    // Store token in request context for UI access
                    context.Items["FacilitatorToken"] = token;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate facilitator token for user");
            }
        }

        await _next(context);
    }
}