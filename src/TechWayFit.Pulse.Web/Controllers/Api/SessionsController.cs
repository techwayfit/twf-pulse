using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Extensions;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Hubs;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IAuthenticationService _authService;
    private readonly IActivityService _activities;
    private readonly IParticipantService _participants;
    private readonly IResponseService _responses;
    private readonly IDashboardService _dashboards;
    private readonly IPollDashboardService _pollDashboards;
    private readonly IWordCloudDashboardService _wordCloudDashboards;
    private readonly IRatingDashboardService _ratingDashboards;
    private readonly IGeneralFeedbackDashboardService _generalFeedbackDashboards;
    private readonly IFacilitatorTokenStore _facilitatorTokens;
    private readonly IParticipantTokenStore _participantTokens;
    private readonly ISessionCodeGenerator _codeGenerator;
    private readonly IHubContext<WorkshopHub, IWorkshopClient> _hub;
    private readonly TechWayFit.Pulse.Application.Abstractions.Services.IParticipantAIService _participantAI;
    private readonly TechWayFit.Pulse.Application.Abstractions.Services.IFacilitatorAIService _facilitatorAI;
    private readonly TechWayFit.Pulse.Application.Abstractions.Services.IAIWorkQueue _aiQueue;
    private readonly TechWayFit.Pulse.Application.Abstractions.Services.ISessionAIService _sessionAI;
    private readonly ISessionGroupService _sessionGroups;
    private readonly TechWayFit.Pulse.Web.Services.IHubNotificationService _hubNotifications;

    public SessionsController(
        ISessionService sessions,
        IAuthenticationService authService,
        IActivityService activities,
        IParticipantService participants,
        IResponseService responses,
        IDashboardService dashboards,
        IPollDashboardService pollDashboards,
        IWordCloudDashboardService wordCloudDashboards,
        IRatingDashboardService ratingDashboards,
        IGeneralFeedbackDashboardService generalFeedbackDashboards,
        IFacilitatorTokenStore facilitatorTokens,
        IParticipantTokenStore participantTokens,
        ISessionCodeGenerator codeGenerator,
        TechWayFit.Pulse.Application.Abstractions.Services.IAIWorkQueue aiQueue,
        TechWayFit.Pulse.Application.Abstractions.Services.ISessionAIService sessionAI,
        IHubContext<WorkshopHub, IWorkshopClient> hub,
        TechWayFit.Pulse.Application.Abstractions.Services.IParticipantAIService participantAI,
        TechWayFit.Pulse.Application.Abstractions.Services.IFacilitatorAIService facilitatorAI,
        ISessionGroupService sessionGroups,
        TechWayFit.Pulse.Web.Services.IHubNotificationService hubNotifications)
    {
        _sessions = sessions;
        _authService = authService;
        _activities = activities;
        _participants = participants;
        _responses = responses;
        _dashboards = dashboards;
        _pollDashboards = pollDashboards;
        _wordCloudDashboards = wordCloudDashboards;
        _ratingDashboards = ratingDashboards;
        _generalFeedbackDashboards = generalFeedbackDashboards;
        _facilitatorTokens = facilitatorTokens;
        _participantTokens = participantTokens;
        _codeGenerator = codeGenerator;
        _hub = hub;
        _participantAI = participantAI;
        _facilitatorAI = facilitatorAI;
        _aiQueue = aiQueue;
        _sessionAI = sessionAI;
        _sessionGroups = sessionGroups;
        _hubNotifications = hubNotifications;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateSessionResponse>>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate the request to catch options field issues
            ValidateJoinFormSchema(request.JoinFormSchema);

            // Always generate a unique code server-side
            var code = await _codeGenerator.GenerateUniqueCodeAsync(cancellationToken);

            var settings = ApiMapper.ToDomain(request.Settings);
            var joinFormSchema = ApiMapper.ToDomain(request.JoinFormSchema);
            var facilitatorUserId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            
            // If no group is specified, use the default group for this facilitator
            var groupId = request.GroupId;
            if (groupId == null && facilitatorUserId.HasValue)
            {
                var defaultGroup = await _sessionGroups.GetDefaultGroupAsync(facilitatorUserId.Value, cancellationToken);
                groupId = defaultGroup?.Id;
            }
            
            var session = await _sessions.CreateSessionAsync(
                code,
                request.Title,
                request.Goal,
                request.Context,
                settings,
                joinFormSchema,
                DateTimeOffset.UtcNow,
                facilitatorUserId,
                groupId,
                cancellationToken);

            return Ok(Wrap(new CreateSessionResponse(session.Id, session.Code)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<CreateSessionResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<CreateSessionResponse>("validation_error", ex.Message));
        }
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgendaActivityResponse>>>> GenerateSessionActivities(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var generated = await _sessionAI.GenerateSessionActivitiesAsync(request, cancellationToken);
            return Ok(Wrap<IReadOnlyList<AgendaActivityResponse>>(generated));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to generate session activities", detail = ex.Message });
        }
    }

    /// <summary>
    /// Generate and add AI activities to an existing session
    /// Optimized endpoint for add-activities page
    /// </summary>
    [HttpPost("{code}/generate-activities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgendaActivityResponse>>>> GenerateActivitiesForSession(
        string code,
        [FromBody] GenerateActivitiesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the session
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<IReadOnlyList<AgendaActivityResponse>>("not_found", "Session not found."));
            }

            // Verify facilitator owns this session
            var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (userId == null || session.FacilitatorUserId != userId)
            {
                return Forbid();
            }

            // Build enhanced context from request parameters
            var enhancedContext = BuildEnhancedContext(
                request.AdditionalContext,
                request.ParticipantCount,
                request.ParticipantType);

            // Calculate target activity count based on duration (1 activity per 5-10 minutes)
            // If duration provided, calculate as duration / 7 (average of 5-10 minutes)
            // If TargetActivityCount explicitly provided, use it (for backward compatibility)
            // Otherwise default to 6 activities
            var targetCount = request.TargetActivityCount 
                ?? (request.DurationMinutes.HasValue ? Math.Max(2, request.DurationMinutes.Value / 7) : 6);

            // Generate and add activities to the session
            var generated = await _sessionAI.GenerateAndAddActivitiesToSessionAsync(
                session,
                enhancedContext,
                request.WorkshopType,
                targetCount,
                request.DurationMinutes,
                request.ExistingActivities,
                cancellationToken);

            return Ok(Wrap<IReadOnlyList<AgendaActivityResponse>>(generated));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("quota"))
        {
            return StatusCode(429, Error<IReadOnlyList<AgendaActivityResponse>>("quota_exceeded", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Error<IReadOnlyList<AgendaActivityResponse>>("generation_failed", $"Failed to generate activities: {ex.Message}"));
        }
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> GetSession(
 string code,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<SessionSummaryResponse>("not_found", "Session not found."));
        }

        return Ok(Wrap(ApiMapper.ToSummary(session)));
    }

    [HttpGet("{code}/participants/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetParticipantCount(
        string code,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<int>("not_found", "Session not found."));
        }

        var participants = await _participants.GetBySessionAsync(session.Id, cancellationToken);
        return Ok(Wrap(participants.Count));
    }

    [HttpGet("{code}/activities/{activityId:guid}/participants/{participantId:guid}/responses/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetParticipantActivityResponseCount(
        string code,
        Guid activityId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<int>("not_found", "Session not found."));
        }

        // Ensure activity belongs to session
        var activities = await _activities.GetAgendaAsync(session.Id, cancellationToken);
        var activity = activities.FirstOrDefault(a => a.Id == activityId);
        if (activity is null)
        {
            return NotFound(Error<int>("not_found", "Activity not found for this session."));
        }

        // Use response service to fetch participant responses for this session and count matches
        var responses = await _responses.GetByParticipantAsync(session.Id, participantId, cancellationToken);
        var count = responses.Count(r => r.ActivityId == activityId);

        return Ok(Wrap(count));
    }

    [HttpPost("{code}/facilitators/join")]
    public async Task<ActionResult<ApiResponse<JoinFacilitatorResponse>>> JoinFacilitator(
        string code,  [FromBody] JoinFacilitatorRequest request,        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<JoinFacilitatorResponse>("not_found", "Session not found."));
        }

        // SECURITY: Only allow authenticated facilitators who own this session to mint tokens
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null || session.FacilitatorUserId != userId)
        {
            return Unauthorized(Error<JoinFacilitatorResponse>("unauthorized", "Only the session owner can join as facilitator."));
        }

        var auth = _facilitatorTokens.Create(session.Id);
        return Ok(Wrap(new JoinFacilitatorResponse(auth.FacilitatorId, auth.Token)));
    }

    [HttpPost("{code}/start")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> StartSession(
        string code,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<SessionSummaryResponse>("not_found", "Session not found."));
        }

        var authError = RequireFacilitatorToken<SessionSummaryResponse>(session);
        if (authError is not null)
        {
            return authError;
        }

        await _sessions.SetStatusAsync(session.Id, TechWayFit.Pulse.Domain.Enums.SessionStatus.Live, DateTimeOffset.UtcNow, cancellationToken);
        var updated = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (updated is not null)
        {
            await _hubNotifications.PublishSessionStateChangedAsync(updated, cancellationToken);
        }
        return Ok(Wrap(ApiMapper.ToSummary(updated ?? session)));
    }

    [HttpPost("{code}/end")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> EndSession(
        string code,
    CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<SessionSummaryResponse>("not_found", "Session not found."));
        }

        var authError = RequireFacilitatorToken<SessionSummaryResponse>(session);
        if (authError is not null)
        {
            return authError;
        }

        await _sessions.SetStatusAsync(session.Id, TechWayFit.Pulse.Domain.Enums.SessionStatus.Ended, DateTimeOffset.UtcNow, cancellationToken);
        var updated = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (updated is not null)
        {
            await _hubNotifications.PublishSessionStateChangedAsync(updated, cancellationToken);
        }
        return Ok(Wrap(ApiMapper.ToSummary(updated ?? session)));
    }

    [HttpPut("{code}/join-form")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> UpdateJoinForm(
     string code,
        [FromBody] UpdateJoinFormRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<SessionSummaryResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<SessionSummaryResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            var schema = ApiMapper.ToDomain(request.JoinFormSchema);
            var updated = await _sessions.UpdateJoinFormSchemaAsync(session.Id, schema, DateTimeOffset.UtcNow, cancellationToken);
            return Ok(Wrap(ApiMapper.ToSummary(updated)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
    }

    [HttpPut("{code}/group")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> AssignToGroup(
        string code,
        [FromBody] AssignSessionToGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<SessionSummaryResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<SessionSummaryResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            await _sessions.SetSessionGroupAsync(session.Id, request.GroupId, DateTimeOffset.UtcNow, cancellationToken);
            
            var updated = await _sessions.GetByCodeAsync(code, cancellationToken);
            return Ok(Wrap(ApiMapper.ToSummary(updated ?? session)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
    }

    [HttpPost("{code}/activities")]
    public async Task<ActionResult<ApiResponse<ActivityResponse>>> AddActivity(
        string code,
        [FromBody] AddActivityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<ActivityResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<ActivityResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            // Auto-assign order if not provided (append to end)
            var order = request.Order;
            if (!order.HasValue || order.Value <= 0)
            {
                var existingActivities = await _activities.GetAgendaAsync(session.Id, cancellationToken);
                order = existingActivities.Count + 1;
            }

            var activity = await _activities.AddActivityAsync(
                session.Id,
                order.Value,
            ApiMapper.MapActivityType(request.Type),
                      request.Title,
            request.Prompt,
            request.Config,
                request.DurationMinutes,
                cancellationToken);

            await _hubNotifications.PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
            return Ok(Wrap(new ActivityResponse(activity.Id)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
    }

    [HttpPut("{code}")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> UpdateSession(
        string code,
        [FromBody] UpdateSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<SessionSummaryResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<SessionSummaryResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            var sessionId = session.Id;

            // Update basic session info (title, goal, context)
            await _sessions.UpdateSessionAsync(
                sessionId,
                request.Title,
                request.Goal,
                request.Context,
                DateTimeOffset.UtcNow,
                cancellationToken);

            // Update group assignment if provided
            var currentSessionForGroup = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (currentSessionForGroup is not null && request.GroupId != currentSessionForGroup.GroupId)
            {
                await _sessions.SetSessionGroupAsync(
                    sessionId,
                    request.GroupId,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
            }

            // Update session schedule if provided
            if (request.SessionStart.HasValue || request.SessionEnd.HasValue)
            {
                var currentSessionForSchedule = await _sessions.GetByCodeAsync(code, cancellationToken);
                if (currentSessionForSchedule is not null)
                {
                    await _sessions.SetSessionScheduleAsync(
                        sessionId,
                        request.SessionStart ?? currentSessionForSchedule.SessionStart,
                        request.SessionEnd ?? currentSessionForSchedule.SessionEnd,
                        DateTimeOffset.UtcNow,
                        cancellationToken);
                }
            }

            // Update settings if any boolean fields are provided
            if (request.AllowAnonymous.HasValue || request.StrictCurrentActivityOnly.HasValue)
            {
                var currentSessionForSettings = await _sessions.GetByCodeAsync(code, cancellationToken);
                if (currentSessionForSettings is not null)
                {
                    var settings = new Domain.ValueObjects.SessionSettings(
                        strictCurrentActivityOnly: request.StrictCurrentActivityOnly ?? currentSessionForSettings.Settings.StrictCurrentActivityOnly,
                        allowAnonymous: request.AllowAnonymous ?? currentSessionForSettings.Settings.AllowAnonymous,
                        ttlMinutes: request.TtlMinutes ?? currentSessionForSettings.Settings.TtlMinutes
                    );

                    await _sessions.UpdateSessionSettingsAsync(
                        sessionId,
                        settings,
                        DateTimeOffset.UtcNow,
                        cancellationToken);
                }
            }

            // Fetch final updated session to return
            var finalSession = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (finalSession is null)
            {
                throw new InvalidOperationException("Session not found after update.");
            }

            return Ok(Wrap(ApiMapper.ToSummary(finalSession)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
    }

    [HttpPut("{code}/settings")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> UpdateSessionSettings(
        string code,
        [FromBody] UpdateSessionSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<SessionSummaryResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<SessionSummaryResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            var settings = new TechWayFit.Pulse.Domain.ValueObjects.SessionSettings( 
                request.StrictCurrentActivityOnly,
                request.AllowAnonymous,
                request.TtlMinutes);

            var updatedSession = await _sessions.UpdateSessionSettingsAsync(
                session.Id,
                settings,
                DateTimeOffset.UtcNow,
                cancellationToken);

            return Ok(Wrap(ApiMapper.ToSummary(updatedSession)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<SessionSummaryResponse>("validation_error", ex.Message));
        }
    }

    [HttpPut("{code}/activities/{activityId:guid}")]
    public async Task<ActionResult<ApiResponse<ActivityResponse>>> UpdateActivity(
        string code,
        Guid activityId,
        [FromBody] UpdateActivityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<ActivityResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<ActivityResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            var activity = await _activities.UpdateActivityAsync(
                session.Id,
                activityId,
                request.Title,
                request.Prompt,
                request.Config,
                request.DurationMinutes,
                cancellationToken);

            await _hubNotifications.PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
            return Ok(Wrap(new ActivityResponse(activity.Id)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
    }

    [HttpDelete("{code}/activities/{activityId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteActivity(
        string code,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<object>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<object>(session);
            if (authError is not null)
            {
                return authError;
            }

            await _activities.DeleteActivityAsync(
                session.Id,
                activityId,
                cancellationToken);

            await _hub.Clients.Group(session.Code).ActivityDeleted(activityId);
            return Ok(Wrap(new { message = "Activity deleted successfully" }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<object>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<object>("validation_error", ex.Message));
        }
    }

    [HttpGet("{code}/activities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgendaActivityResponse>>>> GetAgenda(
        string code,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<IReadOnlyList<AgendaActivityResponse>>("not_found", "Session not found."));
        }

        var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
        var response = agenda
    .OrderBy(activity => activity.Order)
         .Select(ApiMapper.ToAgenda)
       .ToList();
        return Ok(Wrap<IReadOnlyList<AgendaActivityResponse>>(response));
    }

    [HttpPut("{code}/activities/reorder")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgendaActivityResponse>>>> ReorderActivities(
 string code,
[FromBody] ReorderActivitiesRequest request,
   CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<IReadOnlyList<AgendaActivityResponse>>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<IReadOnlyList<AgendaActivityResponse>>(session);
            if (authError is not null)
            {
                return authError;
            }

            var updated = await _activities.ReorderAsync(session.Id, request.ActivityIds, cancellationToken);
            var response = updated
     .OrderBy(activity => activity.Order)
                  .Select(ApiMapper.ToAgenda)
           .ToList();
            return Ok(Wrap<IReadOnlyList<AgendaActivityResponse>>(response));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<IReadOnlyList<AgendaActivityResponse>>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<IReadOnlyList<AgendaActivityResponse>>("validation_error", ex.Message));
        }
    }

    [HttpPost("{code}/activities/{activityId:guid}/open")]
    public async Task<ActionResult<ApiResponse<ActivityResponse>>> OpenActivity(
        string code,
     Guid activityId,
      CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<ActivityResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<ActivityResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            await _activities.OpenAsync(session.Id, activityId, DateTimeOffset.UtcNow, cancellationToken);
            await _sessions.SetCurrentActivityAsync(session.Id, activityId, DateTimeOffset.UtcNow, cancellationToken);
            var updated = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (updated is not null)
            {
                await _hubNotifications.PublishSessionStateChangedAsync(updated, cancellationToken);
            }

            var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var activity = agenda.FirstOrDefault(item => item.Id == activityId);
            if (activity is not null)
            {
                await _hubNotifications.PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
            }
            return Ok(Wrap(new ActivityResponse(activityId)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
    }

    [HttpPost("{code}/activities/{activityId:guid}/reopen")]
    public async Task<ActionResult<ApiResponse<ActivityResponse>>> ReopenActivity(
        string code,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<ActivityResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<ActivityResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            // Close current activity if one is open
            if (session.CurrentActivityId.HasValue && session.CurrentActivityId.Value != activityId)
            {
                await _activities.CloseAsync(session.Id, session.CurrentActivityId.Value, DateTimeOffset.UtcNow, cancellationToken);
            }

            await _activities.ReopenAsync(session.Id, activityId, DateTimeOffset.UtcNow, cancellationToken);
            await _sessions.SetCurrentActivityAsync(session.Id, activityId, DateTimeOffset.UtcNow, cancellationToken);
            var updated = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (updated is not null)
            {
                await _hubNotifications.PublishSessionStateChangedAsync(updated, cancellationToken);
            }

            var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var activity = agenda.FirstOrDefault(item => item.Id == activityId);
            if (activity is not null)
            {
                await _hubNotifications.PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
            }
            return Ok(Wrap(new ActivityResponse(activityId)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
    }

    [HttpPost("{code}/activities/{activityId:guid}/close")]
    public async Task<ActionResult<ApiResponse<ActivityResponse>>> CloseActivity(
 string code,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<ActivityResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<ActivityResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            await _activities.CloseAsync(session.Id, activityId, DateTimeOffset.UtcNow, cancellationToken);
            await _sessions.SetCurrentActivityAsync(session.Id, null, DateTimeOffset.UtcNow, cancellationToken);
            var updated = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (updated is not null)
            {
                await _hubNotifications.PublishSessionStateChangedAsync(updated, cancellationToken);
            }

            var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var activity = agenda.FirstOrDefault(item => item.Id == activityId);
            if (activity is not null)
            {
                await _hubNotifications.PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
            }
            return Ok(Wrap(new ActivityResponse(activityId)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<ActivityResponse>("validation_error", ex.Message));
        }
    }

    [HttpPost("{code}/participants/join")]
    public async Task<ActionResult<ApiResponse<JoinParticipantResponse>>> JoinParticipant(
       string code,
          [FromBody] JoinParticipantRequest request,
          CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<JoinParticipantResponse>("not_found", "Session not found."));
            }

            var participant = await _participants.JoinAsync(
                session.Id,
        request.DisplayName,
         request.IsAnonymous,
          request.Dimensions ?? new Dictionary<string, string?>(),
       DateTimeOffset.UtcNow,
    cancellationToken);

            var participantCount = await _participants.GetBySessionAsync(session.Id, cancellationToken);

            // Broadcast participant joined event
            await _hub.Clients.Group(session.Code).ParticipantJoined(new ParticipantJoinedEvent(
        session.Code,
            participant.Id,
 participant.DisplayName,
            participantCount.Count,
                DateTimeOffset.UtcNow));

            // SECURITY: Return the participant's token (generated during creation and persisted to database)
            // Add to in-memory cache for performance
            if (!string.IsNullOrEmpty(participant.Token))
            {
                _participantTokens.TryGet(session.Id, participant.Id, out _); // This will cache it from DB
            }
            
            return Ok(Wrap(new JoinParticipantResponse(participant.Id, participant.Token ?? throw new InvalidOperationException("Participant token not generated"))));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<JoinParticipantResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<JoinParticipantResponse>("validation_error", ex.Message));
        }
    }

    [HttpPost("{code}/activities/{activityId:guid}/responses")]
    public async Task<ActionResult<ApiResponse<SubmitResponseResponse>>> SubmitResponse(
        string code,
        Guid activityId,
        [FromBody] SubmitResponseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<SubmitResponseResponse>("not_found", "Session not found."));
            }

            // SECURITY: Validate participant token to prevent identity spoofing
            var tokenValidationResult = RequireParticipantToken<SubmitResponseResponse>(session.Id, request.ParticipantId);
            if (tokenValidationResult is not null)
            {
                return tokenValidationResult;
            }

            var response = await _responses.SubmitAsync(
          session.Id,
        activityId,
    request.ParticipantId,
           request.Payload,
       DateTimeOffset.UtcNow,
        cancellationToken);

            // Broadcast response received event
            await _hub.Clients.Group(session.Code).ResponseReceived(new ResponseReceivedEvent(
       session.Code,
                   activityId,
              response.Id,
        request.ParticipantId,
           response.CreatedAt,
      DateTimeOffset.UtcNow));

            // Broadcast dashboard updated event
            await _hub.Clients.Group(session.Code).DashboardUpdated(new DashboardUpdatedEvent(
                   session.Code,
              activityId,
            "response_submitted",
             new { ResponseId = response.Id, ActivityId = activityId },
            DateTimeOffset.UtcNow));

            return Ok(Wrap(new SubmitResponseResponse(response.Id)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<SubmitResponseResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<SubmitResponseResponse>("validation_error", ex.Message));
        }
    }

    [HttpGet("{code}/participants/{participantId:guid}/responses")]
    public async Task<ActionResult<ApiResponse<ParticipantResponsesResponse>>> GetParticipantResponses(
  string code,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<ParticipantResponsesResponse>("not_found", "Session not found."));
        }

        var participants = await _participants.GetBySessionAsync(session.Id, cancellationToken);
        if (participants.All(participant => participant.Id != participantId))
        {
            return NotFound(Error<ParticipantResponsesResponse>("not_found", "Participant not found."));
        }

        var responses = await _responses.GetByParticipantAsync(session.Id, participantId, cancellationToken);
        var items = responses
         .Select(response => new ParticipantResponseItem(
        response.Id,
              response.ActivityId,
           response.Payload,
               response.CreatedAt))
     .ToList();

        return Ok(Wrap(new ParticipantResponsesResponse(participantId, items)));
    }

    [HttpGet("{code}/participants/{participantId:guid}/dashboard")]
    public async Task<ActionResult<ApiResponse<ParticipantDashboardResponse>>> GetParticipantDashboard(
 string code,
  Guid participantId,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<ParticipantDashboardResponse>("not_found", "Session not found."));
        }

        var participants = await _participants.GetBySessionAsync(session.Id, cancellationToken);
        if (participants.All(participant => participant.Id != participantId))
        {
            return NotFound(Error<ParticipantDashboardResponse>("not_found", "Participant not found."));
        }

        var responses = await _responses.GetByParticipantAsync(session.Id, participantId, cancellationToken);
        var totalResponses = responses.Count;
        var distinctActivities = responses.Select(response => response.ActivityId).Distinct().Count();
        var lastResponseAt = responses.Count == 0
      ? (DateTimeOffset?)null
    : responses.Max(response => response.CreatedAt);

        return Ok(Wrap(new ParticipantDashboardResponse(
        session.Id,
            participantId,
   totalResponses,
        distinctActivities,
            lastResponseAt)));
    }

    [HttpGet("{code}/dashboards")]
    public async Task<ActionResult<ApiResponse<DashboardResponse>>> GetDashboard(
        string code,
      [FromQuery] Guid? activityId,
        [FromQuery] Dictionary<string, string?>? filters,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<DashboardResponse>("not_found", "Session not found."));
            }

            var dashboard = await _dashboards.GetDashboardAsync(
       session.Id,
          activityId,
filters ?? new Dictionary<string, string?>(),
      cancellationToken);

            return Ok(Wrap(dashboard));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<DashboardResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<DashboardResponse>("validation_error", ex.Message));
        }
    }

    [HttpGet("{code}/activities/{activityId:guid}/dashboard/poll")]
    public async Task<ActionResult<ApiResponse<PollDashboardResponse>>> GetPollDashboard(
        string code,
        Guid activityId,
        [FromQuery] Dictionary<string, string?>? filters,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<PollDashboardResponse>("not_found", "Session not found."));
            }

            var dashboard = await _pollDashboards.GetPollDashboardAsync(
                session.Id,
                activityId,
                filters ?? new Dictionary<string, string?>(),
                cancellationToken);

            return Ok(Wrap(dashboard));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<PollDashboardResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<PollDashboardResponse>("validation_error", ex.Message));
        }
    }

    [HttpGet("{code}/activities/{activityId:guid}/dashboard/wordcloud")]
    public async Task<ActionResult<ApiResponse<WordCloudDashboardResponse>>> GetWordCloudDashboard(
        string code,
        Guid activityId,
        [FromQuery] Dictionary<string, string?>? filters,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<WordCloudDashboardResponse>("not_found", "Session not found."));
            }

            var dashboard = await _wordCloudDashboards.GetWordCloudDashboardAsync(
                session.Id,
                activityId,
                filters ?? new Dictionary<string, string?>(),
                cancellationToken);

            return Ok(Wrap(dashboard));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<WordCloudDashboardResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<WordCloudDashboardResponse>("validation_error", ex.Message));
        }
    }

    [HttpGet("{code}/activities/{activityId:guid}/dashboard/rating")]
    public async Task<ActionResult<ApiResponse<RatingDashboardResponse>>> GetRatingDashboard(
        string code,
        Guid activityId,
        [FromQuery] Dictionary<string, string?>? filters,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<RatingDashboardResponse>("not_found", "Session not found."));
            }

            var dashboard = await _ratingDashboards.GetRatingDashboardAsync(
                session.Id,
                activityId,
                filters ?? new Dictionary<string, string?>(),
                cancellationToken);

            return Ok(Wrap(dashboard));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<RatingDashboardResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<RatingDashboardResponse>("validation_error", ex.Message));
        }
    }

    [HttpGet("{code}/activities/{activityId:guid}/dashboard/generalfeedback")]
    public async Task<ActionResult<ApiResponse<GeneralFeedbackDashboardResponse>>> GetGeneralFeedbackDashboard(
        string code,
        Guid activityId,
        [FromQuery] Dictionary<string, string?>? filters,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<GeneralFeedbackDashboardResponse>("not_found", "Session not found."));
            }

            var dashboard = await _generalFeedbackDashboards.GetGeneralFeedbackDashboardAsync(
                session.Id,
                activityId,
                filters ?? new Dictionary<string, string?>(),
                cancellationToken);

            return Ok(Wrap(dashboard));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<GeneralFeedbackDashboardResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<GeneralFeedbackDashboardResponse>("validation_error", ex.Message));
        }
    }

    private static ApiResponse<T> Wrap<T>(T data)
    {
        return new ApiResponse<T>(data);
    }

    private static ApiResponse<T> Error<T>(string code, string message)
    {
        return new ApiResponse<T>(default, new[] { new ApiError(code, message) });
    }

    private const string FacilitatorTokenHeader = "X-Facilitator-Token";
    private const string ParticipantTokenHeader = "X-Participant-Token";

    private ActionResult<ApiResponse<T>>? RequireFacilitatorToken<T>(TechWayFit.Pulse.Domain.Entities.Session session)
    {
        // First, check if the current facilitator context user owns this session
        var currentContext = Application.Context.FacilitatorContextAccessor.Current;
        if (currentContext != null && currentContext.FacilitatorUserId == session.FacilitatorUserId)
        {
            // Authenticated facilitator owns this session - allow access
            return null;
        }

        // SECURITY: Require a valid session-specific facilitator token
        // Reject requests when no token exists (do not treat missing tokens as "allowed")
        if (!_facilitatorTokens.TryGet(session.Id, out var auth))
        {
            return Unauthorized(Error<T>("facilitator_token_required", "Facilitator token is required."));
        }

        if (Request.Headers.TryGetValue(FacilitatorTokenHeader, out var token)
     && string.Equals(token.ToString(), auth.Token, StringComparison.Ordinal))
        {
            return null;
        }

        return Unauthorized(Error<T>("facilitator_token_required", "Facilitator token is required."));
    }

    private ActionResult<ApiResponse<T>>? RequireParticipantToken<T>(Guid sessionId, Guid participantId)
    {
        // SECURITY: Validate that the request includes a valid participant token
        if (!Request.Headers.TryGetValue(ParticipantTokenHeader, out var token))
        {
            return Unauthorized(Error<T>("participant_token_required", "Participant token is required."));
        }

        if (!_participantTokens.IsValid(sessionId, participantId, token.ToString()))
        {
            return Unauthorized(Error<T>("invalid_participant_token", "Invalid or mismatched participant token."));
        }

        return null;
    }

    /// <summary>
    /// Build enhanced context by combining user context with participant information
    /// This helps AI generate more relevant activities
    /// </summary>
    private static string? BuildEnhancedContext(
        string? additionalContext,
        int? participantCount,
        string? participantType)
    {
        var contextParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            contextParts.Add(additionalContext.Trim());
        }

        if (participantCount.HasValue)
        {
            var teamSize = participantCount.Value switch
            {
                <= 5 => "small team",
                <= 10 => "medium-sized team",
                <= 20 => "large team",
                _ => "large group"
            };
            contextParts.Add($"Expected audience: {teamSize} (~{participantCount} participants)");
        }

        if (!string.IsNullOrWhiteSpace(participantType))
        {
            var types = participantType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var descriptions = types.Select(type => type.ToLowerInvariant() switch
            {
                "developers" => "developers/engineers",
                "product" => "product managers",
                "designers" => "designers/UX",
                "leadership" => "leadership/executives",
                "operations" => "operations/support",
                "sales" => "sales/customer success",
                _ => type
            }).ToArray();
            
            var audienceDescription = descriptions.Length switch
            {
                1 => $"{descriptions[0]} team",
                2 => $"{descriptions[0]} and {descriptions[1]}",
                _ => $"{string.Join(", ", descriptions.Take(descriptions.Length - 1))}, and {descriptions.Last()}"
            };
            contextParts.Add($"Participant profile: {audienceDescription}");
        }

        return contextParts.Any() ? string.Join(". ", contextParts) : null;
    }

    private void ValidateJoinFormSchema(JoinFormSchemaDto schema)
    {
        if (schema?.Fields == null) return;

        foreach (var field in schema.Fields)
        {
            // Only validate options for dropdown and multiselect fields
            if (field.Type == TechWayFit.Pulse.Contracts.Enums.FieldType.Dropdown ||
              field.Type == TechWayFit.Pulse.Contracts.Enums.FieldType.MultiSelect)
            {
                // Check if options is provided and not empty
                if (string.IsNullOrWhiteSpace(field.Options))
                {
                    throw new ArgumentException($"Field '{field.Label}' of type '{field.Type}' must have options defined (comma-separated values).");
                }

                // Validate that the parsed options list has valid entries
                var parsedOptions = field.OptionsList;
                if (!parsedOptions.Any())
                {
                    throw new ArgumentException($"Field '{field.Label}' of type '{field.Type}' has invalid options format. Expected comma-separated values, got: '{field.Options}'");
                }
            }
            // For other field types (Text, Number, Boolean), options is optional
            // No validation needed
        }
    }
}
