using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers;

[Authorize]
[Route("facilitator")]
public class FacilitatorController : Controller
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionService _sessionService;
    private readonly ISessionGroupService _sessionGroupService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FacilitatorController> _logger;

    public FacilitatorController(
ISessionRepository sessionRepository,
  ISessionService sessionService,
  ISessionGroupService sessionGroupService,
   IAuthenticationService authService,
   ILogger<FacilitatorController> logger)
    {
        _sessionRepository = sessionRepository;
        _sessionService = sessionService;
        _sessionGroupService = sessionGroupService;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var sessions = await _sessionRepository.GetByFacilitatorUserIdAsync(userId.Value, cancellationToken);
        var groups = await _sessionGroupService.GetGroupHierarchyAsync(userId.Value, cancellationToken);

        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        ViewData["Groups"] = groups;

        return View(sessions);
    }


    /// <summary>
    /// Create session page - static form with JavaScript enhancement
    /// </summary>
    [HttpGet("create")]
    public async Task<IActionResult> CreateSession(CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var groups = await _sessionGroupService.GetFacilitatorGroupsAsync(userId.Value, cancellationToken);
        ViewData["Groups"] = groups;

        return View();
    }

    /// <summary>
    /// Session Groups management page
    /// </summary>
    [HttpGet("groups")]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var groups = await _sessionGroupService.GetGroupHierarchyAsync(userId.Value, cancellationToken);
        
        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        
        return View(groups);
    }
}
