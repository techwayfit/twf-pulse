using Microsoft.Extensions.Options;
using TechWayFit.Pulse.Web.Configuration;

namespace TechWayFit.Pulse.Web.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _cspPolicy;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _cspPolicy = options.Value.ContentSecurityPolicy.Build();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] = _cspPolicy;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}
