using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Controllers;

[Authorize]
[Route("facilitator")]
public class FacilitatorController : Controller
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionService _sessionService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FacilitatorController> _logger;

    public FacilitatorController(
ISessionRepository sessionRepository,
  ISessionService sessionService,
   IAuthenticationService authService,
   ILogger<FacilitatorController> logger)
    {
        _sessionRepository = sessionRepository;
        _sessionService = sessionService;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var sessions = await _sessionRepository.GetByFacilitatorUserIdAsync(userId.Value, cancellationToken);

        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;

        return View(sessions);
    }


    /// <summary>
    /// Create session page - static form with JavaScript enhancement
    /// </summary>
    [HttpGet("create")]
    public IActionResult CreateSession()
    {
        return View();
    }

    private async Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = User.FindFirst("FacilitatorUserId")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var user = await _authService.GetFacilitatorAsync(userId, cancellationToken);
            if (user != null)
            {
                return user.Id;
            }
        }

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var user = await _authService.GetFacilitatorByEmailAsync(email, cancellationToken);
            return user?.Id;
        }

        return null;
    }
}
