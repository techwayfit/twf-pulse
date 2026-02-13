using System.Net;

namespace TechWayFit.Pulse.Web.Handlers;

/// <summary>
/// HTTP message handler that forwards authentication cookies from the current HttpContext
/// to outgoing HTTP requests. This enables authenticated API calls from Blazor Server components.
/// </summary>
public class AuthenticationCookieHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationCookieHandler> _logger;

    public AuthenticationCookieHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationCookieHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext != null)
        {
            // Copy authentication cookies from the current HTTP context
            var cookieHeader = httpContext.Request.Headers["Cookie"].ToString();
            
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
                _logger.LogDebug("Forwarded authentication cookies to {RequestUri}", request.RequestUri);
            }
            else
            {
                _logger.LogDebug("No cookies found in HttpContext for {RequestUri}", request.RequestUri);
            }
        }
        else
        {
            _logger.LogWarning("HttpContext is null - cannot forward authentication cookies");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
