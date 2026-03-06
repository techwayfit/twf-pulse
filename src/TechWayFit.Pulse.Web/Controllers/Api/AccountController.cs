using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IPlanService _planService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IPlanService planService,
        IAuthenticationService authService,
        ILogger<AccountController> logger)
    {
  _planService = planService;
 _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get current plan status and quota information
    /// </summary>
    [HttpGet("plan-status")]
    [ProducesResponseType(typeof(ApiResponse<PlanStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PlanStatusResponse>>> GetPlanStatus(
   CancellationToken cancellationToken)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (!userId.HasValue)
   {
 return Unauthorized();
        }

        var status = await _planService.GetPlanStatusAsync(userId.Value, cancellationToken);

    var response = new PlanStatusResponse(
   status.PlanCode,
    status.PlanDisplayName,
            status.SessionsUsed,
            status.SessionsAllowed,
      status.SessionsResetAt,
 status.ExpiresAt,
            status.Status.ToString(),
        new FeatureAccessDto(
      status.Features.AiAssist,
        status.Features.AiSummary,
      status.Features.ActivityAccess.ToDictionary(
       kv => kv.Key.ToString(),
  kv => kv.Value)));

    return Ok(new ApiResponse<PlanStatusResponse>(response));
    }

    private static ApiResponse<T> Error<T>(string code, string message)
    {
      return new ApiResponse<T>(default, new[] { new ApiError(code, message) });
    }
}
