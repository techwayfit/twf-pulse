using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;
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
    [HttpGet("create-session")]
    [HttpGet("create")] // Backward compatibility
    public async Task<IActionResult> CreateSession(Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var groups = await _sessionGroupService.GetFacilitatorGroupsAsync(userId.Value, cancellationToken);
        ViewData["Groups"] = groups;
        ViewData["SelectedGroupId"] = groupId;

        return View();
    }

    /// <summary>
    /// Add Activities page - choose between manual, template, or AI activity creation
    /// </summary>
    [HttpGet("add-activities")]
    public async Task<IActionResult> AddActivities(string code, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Validate session code from query string
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("AddActivities accessed without session code");
            return NotFound();
        }

        // Verify session exists and belongs to user
        var session = await _sessionRepository.GetByCodeAsync(code, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {Code}", code);
            return NotFound();
        }

        if (session.FacilitatorUserId != userId)
        {
            _logger.LogWarning("Unauthorized access attempt to session {Code} by user {UserId}", code, userId);
            return NotFound(); // Don't reveal if session exists
        }

        // Pass session data to view
        ViewData["SessionCode"] = session.Code;
        ViewData["SessionTitle"] = session.Title;
        ViewData["SessionId"] = session.Id;

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
        var allSessions = await _sessionRepository.GetByFacilitatorUserIdAsync(userId.Value, cancellationToken);
        
        // Build view models with session data
        var groupViewModels = groups.Select(g => new Web.Models.GroupWithSessionsViewModel
        {
            Group = g,
            Sessions = allSessions
                .Where(s => s.GroupId == g.Id)
                .OrderBy(s => s.Title)
                .Select(s => new Web.Models.SessionSummary
                {
                    Id = s.Id,
                    Title = s.Title.Length > 50 ? s.Title.Substring(0, 47) + "..." : s.Title,
                    Status = s.Status.ToString(),
                    ExpiresAt = s.ExpiresAt,
                    IsCompleted = s.Status == Domain.Enums.SessionStatus.Ended || 
                                 s.Status == Domain.Enums.SessionStatus.Expired,
                    IsActive = s.Status == Domain.Enums.SessionStatus.Live && s.ExpiresAt > DateTimeOffset.UtcNow,
                    SessionStart = s.SessionStart,
                    SessionEnd = s.SessionEnd
                })
                .ToList(),
            TotalSessionCount = GetTotalSessionCount(g.Id, groups, allSessions)
        }).ToList();
        
        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        
        return View(groupViewModels);
    }

    private int GetTotalSessionCount(Guid groupId, IReadOnlyCollection<SessionGroup> allGroups, IReadOnlyList<Session> allSessions)
    {
        // Count direct sessions
        var count = allSessions.Count(s => s.GroupId == groupId);
        
        // Count sessions in child groups recursively
        var childGroups = allGroups.Where(g => g.ParentGroupId == groupId);
        foreach (var child in childGroups)
        {
            count += GetTotalSessionCount(child.Id, allGroups, allSessions);
        }
        
        return count;
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

    /// <summary>
    /// Serve activity form modals partial view for dynamic loading
    /// </summary>
    [HttpGet("activity-modals")]
    [AllowAnonymous] // Allow Blazor pages to fetch this
    public IActionResult ActivityModals()
    {
        return PartialView("~/Views/Shared/_ActivityFormModals.cshtml");
    }
}
