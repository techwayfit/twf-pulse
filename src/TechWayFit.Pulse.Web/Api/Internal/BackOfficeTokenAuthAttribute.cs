using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TechWayFit.Pulse.Web.Api.Internal;

/// <summary>
/// Action filter that validates the <c>X-BackOffice-Token</c> request header against
/// the <c>BackOffice:ApiToken</c> configuration value.
/// Apply to any controller or action that should only be reachable by the BackOffice app.
/// Returns 401 if the token is missing or wrong; 503 if the token is not configured.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BackOfficeTokenAuthAttribute : Attribute, IActionFilter
{
    public const string HeaderName = "X-BackOffice-Token";
    public const string ConfigKey  = "BackOffice:ApiToken";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedToken = config[ConfigKey];

        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            // Token not configured — surface as 503 so it's obvious during setup
            context.Result = new ObjectResult(new { error = "Cache management API is not configured." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        var providedToken = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedToken) ||
            !string.Equals(providedToken, expectedToken, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing BackOffice token." });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
