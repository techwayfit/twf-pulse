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
    private readonly ISessionTemplateService _sessionTemplateService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FacilitatorController> _logger;

    public FacilitatorController(
ISessionRepository sessionRepository,
  ISessionService sessionService,
  ISessionGroupService sessionGroupService,
   ISessionTemplateService sessionTemplateService,
   IAuthenticationService authService,
   ILogger<FacilitatorController> logger)
    {
        _sessionRepository = sessionRepository;
        _sessionService = sessionService;
        _sessionGroupService = sessionGroupService;
        _sessionTemplateService = sessionTemplateService;
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

    /// <summary>
    /// Template browser page - list all available templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> Templates(CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var templates = await _sessionTemplateService.GetAllTemplatesAsync(cancellationToken);
        
        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        
        return View(templates);
    }

    /// <summary>
    /// Template customization page - customize template before creating session
    /// </summary>
    [HttpGet("templates/{id:guid}/customize")]
    public async Task<IActionResult> CustomizeTemplate(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var template = await _sessionTemplateService.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
        {
            return NotFound();
        }

        var groups = await _sessionGroupService.GetFacilitatorGroupsAsync(userId.Value, cancellationToken);
        
        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        ViewData["Groups"] = groups;
        
        return View(template);
    }
}
