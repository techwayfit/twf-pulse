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
    private readonly ILogger<FacilitatorController> _logger;

    public FacilitatorController(
ISessionRepository sessionRepository,
  ISessionService sessionService,
   ILogger<FacilitatorController> logger)
    {
        _sessionRepository = sessionRepository;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
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

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
