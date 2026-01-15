using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/session-groups")]
[Authorize]
public sealed class SessionGroupsController : ControllerBase
{
    private readonly ISessionGroupService _sessionGroupService;
    private readonly ISessionService _sessionService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<SessionGroupsController> _logger;

    public SessionGroupsController(
        ISessionGroupService sessionGroupService, 
        ISessionService sessionService,
        ISessionRepository sessionRepository,
        IAuthenticationService authService,
        ILogger<SessionGroupsController> logger)
    {
        _sessionGroupService = sessionGroupService;
        _sessionService = sessionService;
        _sessionRepository = sessionRepository;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SessionGroupResponse>> CreateGroup(
        [FromBody] CreateSessionGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilitatorUserId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (facilitatorUserId == null)
                return Unauthorized(new { message = "Facilitator authentication required" });

            var group = await _sessionGroupService.CreateGroupAsync(
                request.Name,
                request.Description,
                request.Level,
                request.ParentGroupId,
                DateTimeOffset.UtcNow,
                facilitatorUserId.Value,
                cancellationToken);

            var response = new SessionGroupResponse(
                group.Id,
                group.Name,
                group.Description,
                group.Level,
                group.ParentGroupId,
                group.CreatedAt,
                group.UpdatedAt);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SessionGroupResponse>>> GetGroups(
        CancellationToken cancellationToken = default)
    {
        var facilitatorUserId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (facilitatorUserId == null)
            return Unauthorized(new { message = "Facilitator authentication required" });

        var groups = await _sessionGroupService.GetFacilitatorGroupsAsync(
            facilitatorUserId.Value,
            cancellationToken);

        var response = groups.Select(g => new SessionGroupResponse(
            g.Id,
            g.Name,
            g.Description,
            g.Level,
            g.ParentGroupId,
            g.CreatedAt,
            g.UpdatedAt)).ToList();

        return Ok(response);
    }

    [HttpGet("hierarchy")]
    public async Task<ActionResult<IReadOnlyCollection<SessionGroupHierarchyResponse>>> GetHierarchy(
        CancellationToken cancellationToken = default)
    {
        var facilitatorUserId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (facilitatorUserId == null)
            return Unauthorized(new { message = "Facilitator authentication required" });

        var groups = await _sessionGroupService.GetGroupHierarchyAsync(
            facilitatorUserId.Value,
            cancellationToken);

        var response = await BuildHierarchyResponse(groups, null, facilitatorUserId.Value, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionGroupResponse>> GetGroup(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var group = await _sessionGroupService.GetGroupAsync(id, cancellationToken);
        if (group == null)
            return NotFound(new { message = "Group not found" });

        var response = new SessionGroupResponse(
            group.Id,
            group.Name,
            group.Description,
            group.Level,
            group.ParentGroupId,
            group.CreatedAt,
            group.UpdatedAt);

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SessionGroupResponse>> UpdateGroup(
        Guid id,
        [FromBody] UpdateSessionGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _sessionGroupService.UpdateGroupAsync(
                id,
                request.Name,
                request.Description,
                DateTimeOffset.UtcNow,
                cancellationToken);

            var response = new SessionGroupResponse(
                group.Id,
                group.Name,
                group.Description,
                group.Level,
                group.ParentGroupId,
                group.CreatedAt,
                group.UpdatedAt);

            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Group not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGroup(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sessionGroupService.DeleteGroupAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<IReadOnlyCollection<SessionGroupResponse>>> GetChildGroups(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var children = await _sessionGroupService.GetChildGroupsAsync(id, cancellationToken);

        var response = children.Select(g => new SessionGroupResponse(
            g.Id,
            g.Name,
            g.Description,
            g.Level,
            g.ParentGroupId,
            g.CreatedAt,
            g.UpdatedAt)).ToList();

        return Ok(response);
    }

    private async Task<List<SessionGroupHierarchyResponse>> BuildHierarchyResponse(
        IReadOnlyCollection<SessionGroup> allGroups,
        Guid? parentId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken)
    {
        var result = new List<SessionGroupHierarchyResponse>();
        var groups = allGroups.Where(g => g.ParentGroupId == parentId).ToList();

        foreach (var group in groups)
        {
            var sessions = await _sessionService.GetSessionsByGroupAsync(group.Id, facilitatorUserId, cancellationToken);
            var children = await BuildHierarchyResponse(allGroups, group.Id, facilitatorUserId, cancellationToken);

            result.Add(new SessionGroupHierarchyResponse(
                group.Id,
                group.Name,
                group.Description,
                group.Level,
                group.ParentGroupId,
                children,
                sessions.Count));
        }

        return result;
    }
    
    [HttpGet("{groupId}/sessions")]
    public async Task<IActionResult> GetGroupSessions(Guid groupId, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return Unauthorized(new { message = "Facilitator not authenticated" });
        }

        try
        {
            var sessions = await _sessionRepository.GetByGroupAsync(groupId, userId.Value, cancellationToken);
            
            var response = sessions.Select(s => new
            {
                id = s.Id,
                code = s.Code,
                title = s.Title,
                goal = s.Goal,
                status = s.Status.ToString(),
                createdAt = s.CreatedAt.ToString("MMM dd, yyyy"),
                expiresAt = s.ExpiresAt.ToString("MMM dd, yyyy HH:mm"),
                groupId = s.GroupId
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions for group {GroupId}", groupId);
            return StatusCode(500, new { message = "Failed to get group sessions" });
        }
    }
}