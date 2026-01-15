using System.Security.Claims;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the current facilitator user ID from the authenticated HttpContext.
    /// Returns null if user is not authenticated or facilitator is not found.
    /// </summary>
    public static async Task<Guid?> GetFacilitatorUserIdAsync(
        this HttpContext httpContext,
        IAuthenticationService authService,
        CancellationToken cancellationToken = default)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = httpContext.User.FindFirst("FacilitatorUserId")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var user = await authService.GetFacilitatorAsync(userId, cancellationToken);
            if (user != null)
            {
                return user.Id;
            }
        }

        var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var user = await authService.GetFacilitatorByEmailAsync(email, cancellationToken);
            return user?.Id;
        }

        return null;
    }

    /// <summary>
    /// Checks if the current user is authenticated as a facilitator.
    /// </summary>
    public static bool IsFacilitatorAuthenticated(this HttpContext httpContext)
    {
        return httpContext.User?.Identity?.IsAuthenticated == true &&
               (httpContext.User.FindFirst("FacilitatorUserId") != null ||
                httpContext.User.FindFirst(ClaimTypes.Email) != null);
    }
}