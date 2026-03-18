namespace TechWayFit.Pulse.Web.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private const string CspPolicy = "default-src 'self'; "
        + "base-uri 'self'; "
        + "frame-ancestors 'self'; "
        + "form-action 'self'; "
        + "img-src 'self' data: blob: https:; "
        + "font-src 'self' data: https://fonts.gstatic.com; "
        + "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; "
        + "script-src 'self' 'unsafe-inline' 'unsafe-eval'; "
        + "connect-src 'self' https: wss: ws:;";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] = CspPolicy;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}
