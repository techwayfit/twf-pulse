using Microsoft.AspNetCore.Http;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Context;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Web.Services;

namespace TechWayFit.Pulse.Web.Middleware;

/// <summary>
/// Middleware that populates the FacilitatorContext from the authentication token
/// </summary>
public sealed class FacilitatorContextMiddleware
{
    private readonly RequestDelegate _next;

    public FacilitatorContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthenticationService authService,
        IFacilitatorTokenService tokenService,
        IFacilitatorUserDataRepository userDataRepository,
        IApiKeyProtectionService apiKeyProtection)
    {
        try
        {
            FacilitatorContext? facilitatorContext = null;

            // First, check if user is authenticated via ASP.NET Core authentication
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var emailClaim = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
                if (emailClaim != null)
                {
                    var facilitator = await authService.GetFacilitatorByEmailAsync(emailClaim.Value, default);
                    if (facilitator != null)
                    {
                        var userData = await userDataRepository.GetAllAsDictAsync(facilitator.Id, default);
                        facilitatorContext = new FacilitatorContext
                        {
                            FacilitatorUserId = facilitator.Id,
                            Email = facilitator.Email,
                            DisplayName = facilitator.DisplayName,
                            OpenAiApiKey = apiKeyProtection.TryUnprotect(userData.GetValueOrDefault(FacilitatorUserDataKeys.OpenAiApiKey)),
                            OpenAiBaseUrl = userData.GetValueOrDefault(FacilitatorUserDataKeys.OpenAiBaseUrl)
                        };
                    }
                }
            }

            // If not authenticated via ASP.NET Core, try facilitator token from cookie or header
            if (facilitatorContext == null)
            {
                var token = context.Request.Cookies["FacilitatorToken"]
                    ?? context.Request.Headers["X-Facilitator-Token"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    var facilitatorUserId = await tokenService.ValidateTokenAsync(token);
                    if (facilitatorUserId.HasValue)
                    {
                        var facilitator = await authService.GetFacilitatorAsync(facilitatorUserId.Value, default);
                        if (facilitator != null)
                        {
                            // Load user-specific data (OpenAI credentials, etc.)
                            var userData = await userDataRepository.GetAllAsDictAsync(facilitator.Id, default);
                            
                            facilitatorContext = new FacilitatorContext
                            {
                                FacilitatorUserId = facilitator.Id,
                                Email = facilitator.Email,
                                DisplayName = facilitator.DisplayName,
                                OpenAiApiKey = apiKeyProtection.TryUnprotect(userData.GetValueOrDefault(FacilitatorUserDataKeys.OpenAiApiKey)),
                                OpenAiBaseUrl = userData.GetValueOrDefault(FacilitatorUserDataKeys.OpenAiBaseUrl)
                            };
                        }
                    }
                }
            }

            // Set the context if we found one
            if (facilitatorContext != null)
            {
                FacilitatorContextAccessor.Set(facilitatorContext);
            }

            await _next(context);
        }
        finally
        {
            // Clear context after request completes
            FacilitatorContextAccessor.Clear();
        }
    }
}
