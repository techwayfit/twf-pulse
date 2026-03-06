using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/promo-codes")]
[Authorize] // Require authentication
public class PromoCodesController : ControllerBase
{
    private readonly IPromoCodeService _promoCodeService;
 private readonly IAuthenticationService _authService;
 private readonly ILogger<PromoCodesController> _logger;

    public PromoCodesController(
IPromoCodeService promoCodeService,
 IAuthenticationService authService,
   ILogger<PromoCodesController> logger)
    {
      _promoCodeService = promoCodeService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Validate a promo code without redeeming it
    /// </summary>
    /// <param name="request">Promo code to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with plan details</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<ValidatePromoCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidatePromoCodeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<ApiResponse<ValidatePromoCodeResponse>>> ValidateCode(
        [FromBody] ValidatePromoCodeRequest request,
        CancellationToken ct)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, ct);
        if (!userId.HasValue)
        {
            _logger.LogWarning("Promo code validation attempted by unauthenticated user");
            return Unauthorized();
        }

        var result = await _promoCodeService.ValidateCodeAsync(request.Code, userId.Value, ct);

        var response = new ValidatePromoCodeResponse(
      result.IsValid,
        result.ErrorMessage,
       result.PlanDisplayName,
       result.DurationDays);

  if (!result.IsValid)
    {
            return Ok(new ApiResponse<ValidatePromoCodeResponse>(
   response,
     new[] { new ApiError("invalid_code", result.ErrorMessage ?? "Invalid promo code") }));
      }

        return Ok(new ApiResponse<ValidatePromoCodeResponse>(response));
    }

    /// <summary>
    /// Redeem a promo code and apply promotional subscription
    /// </summary>
    /// <param name="request">Promo code to redeem</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription details</returns>
    [HttpPost("redeem")]
    [ProducesResponseType(typeof(ApiResponse<RedeemPromoCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RedeemPromoCodeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 public async Task<ActionResult<ApiResponse<RedeemPromoCodeResponse>>> RedeemCode(
        [FromBody] RedeemPromoCodeRequest request,
        CancellationToken ct)
    {
   var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, ct);
   if (!userId.HasValue)
 {
        _logger.LogWarning("Promo code redemption attempted by unauthenticated user");
   return Unauthorized();
        }

        try
        {
   var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
 var subscription = await _promoCodeService.RedeemCodeAsync(request.Code, userId.Value, ipAddress, ct);

     var message = $"Promo code applied! You now have premium access until {subscription.ExpiresAt!.Value:MMM dd, yyyy}.";

   return Ok(new ApiResponse<RedeemPromoCodeResponse>(
         new RedeemPromoCodeResponse(
      subscription.Id,
           subscription.PlanId,
          subscription.ExpiresAt.Value,
  message)));
        }
        catch (InvalidOperationException ex)
    {
      _logger.LogWarning("Promo code redemption failed for user {UserId}: {Error}", 
userId.Value, ex.Message);
            
  return BadRequest(new ApiResponse<RedeemPromoCodeResponse>(
                null,
                new[] { new ApiError("invalid_code", ex.Message) }));
        }
    catch (Exception ex)
        {
     _logger.LogError(ex, "Unexpected error during promo code redemption for user {UserId}", userId.Value);
       
            return StatusCode(500, new ApiResponse<RedeemPromoCodeResponse>(
     null,
 new[] { new ApiError("server_error", "An error occurred while redeeming the promo code. Please try again.") }));
        }
    }
}
