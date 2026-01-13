using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthApiController(IAuthenticationService authService)
    {
   _authService = authService;
    }

    [HttpGet("current-user")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
     {
     return Unauthorized();
        }

      var user = await _authService.GetFacilitatorAsync(userId);

      if (user == null)
        {
   return NotFound(new { message = "User not found" });
        }

        return Ok(new
    {
   id = user.Id,
     email = user.Email,
     displayName = user.DisplayName,
       createdAt = user.CreatedAt,
 lastLoginAt = user.LastLoginAt
        });
    }

    [HttpGet("check-auth")]
    public IActionResult CheckAuth()
    {
return Ok(new
        {
       isAuthenticated = User.Identity?.IsAuthenticated ?? false,
   userName = User.FindFirst(ClaimTypes.Name)?.Value,
   userEmail = User.FindFirst(ClaimTypes.Email)?.Value
    });
 }
}
