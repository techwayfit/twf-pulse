using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
public sealed class SessionAiController : SessionApiControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IActivityService _activities;
    private readonly IAuthenticationService _authService;
    private readonly ISessionAIService _sessionAI;
    private readonly TechWayFit.Pulse.Web.Services.IHubNotificationService _hubNotifications;
    private readonly ILogger<SessionAiController> _logger;

    public SessionAiController(
        ISessionService sessions,
        IActivityService activities,
        IAuthenticationService authService,
        ISessionAIService sessionAI,
        IFacilitatorTokenStore facilitatorTokens,
        TechWayFit.Pulse.Web.Services.IHubNotificationService hubNotifications,
        ILogger<SessionAiController> logger)
        : base(facilitatorTokens: facilitatorTokens)
    {
        _sessions = sessions;
        _activities = activities;
        _authService = authService;
        _sessionAI = sessionAI;
        _hubNotifications = hubNotifications;
        _logger = logger;
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

    [HttpPost("{code}/generate-activities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgendaActivityResponse>>>> GenerateActivitiesForSession(
        string code,
        [FromBody] GenerateActivitiesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<IReadOnlyList<AgendaActivityResponse>>("not_found", "Session not found."));
            }

            var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (userId == null || session.FacilitatorUserId != userId)
            {
                return Forbid();
            }

            var enhancedContext = BuildEnhancedContext(
                request.AdditionalContext,
                request.ParticipantCount,
                request.ParticipantType);

            var targetCount = request.TargetActivityCount
                ?? (request.DurationMinutes.HasValue ? Math.Max(2, request.DurationMinutes.Value / 7) : 6);

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

    [HttpPost("{code}/activities/{activityId:guid}/generate-summary")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateAiSummary(
        string code,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<string>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<string>(session);
            if (authError is not null)
            {
                return authError;
            }

            var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var activity = agenda.FirstOrDefault(a => a.Id == activityId);
            if (activity is null)
            {
                return NotFound(Error<string>("not_found", "Activity not found."));
            }

            if (activity.Type != TechWayFit.Pulse.Domain.Enums.ActivityType.AiSummary)
            {
                return BadRequest(Error<string>("invalid_type", "Only AiSummary activities can generate a summary."));
            }

            var completedActivities = agenda
                .Where(a => a.Status == TechWayFit.Pulse.Domain.Enums.ActivityStatus.Closed
                         && a.Type != TechWayFit.Pulse.Domain.Enums.ActivityType.AiSummary
                         && a.Type != TechWayFit.Pulse.Domain.Enums.ActivityType.Break)
                .OrderBy(a => a.Order)
                .Select(ApiMapper.ToAgenda)
                .ToList();

            string? customPromptAddition = null;
            if (!string.IsNullOrEmpty(activity.Config))
            {
                try
                {
                    using var configDoc = System.Text.Json.JsonDocument.Parse(activity.Config);
                    if (configDoc.RootElement.TryGetProperty("customPromptAddition", out var cpa))
                    {
                        customPromptAddition = cpa.GetString();
                    }
                }
                catch
                {
                }
            }

            var generatingConfig = System.Text.Json.JsonSerializer.Serialize(
                new TechWayFit.Pulse.Domain.Models.ActivityConfigs.AiSummaryConfig
                {
                    CustomPromptAddition = customPromptAddition ?? string.Empty,
                    IsGenerating = true,
                    GeneratedSummary = string.Empty
                },
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

            await _activities.UpdateActivityAsync(session.Id, activityId, activity.Title, activity.Prompt, generatingConfig, activity.DurationMinutes, cancellationToken);
            var updatedAgenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var updatingActivity = updatedAgenda.FirstOrDefault(a => a.Id == activityId);
            if (updatingActivity != null)
            {
                await _hubNotifications.PublishActivityStateChangedAsync(session.Code, updatingActivity, cancellationToken);
            }

            string summaryText;
            try
            {
                summaryText = await _sessionAI.GenerateSessionSummaryAsync(
                    session.Title,
                    session.Goal,
                    completedActivities,
                    customPromptAddition,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI summary for activity {ActivityId}", activityId);
                summaryText = $"## Session Summary\\n\\nAI summary generation encountered an error. Please try again.\\n\\n*Error: {ex.Message}*";
            }

            var finalConfig = System.Text.Json.JsonSerializer.Serialize(
                new TechWayFit.Pulse.Domain.Models.ActivityConfigs.AiSummaryConfig
                {
                    CustomPromptAddition = customPromptAddition ?? string.Empty,
                    IsGenerating = false,
                    GeneratedSummary = summaryText,
                    GeneratedAt = DateTimeOffset.UtcNow
                },
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

            await _activities.UpdateActivityAsync(session.Id, activityId, activity.Title, activity.Prompt, finalConfig, activity.DurationMinutes, cancellationToken);

            var finalAgenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var finalActivity = finalAgenda.FirstOrDefault(a => a.Id == activityId);
            if (finalActivity != null)
            {
                await _hubNotifications.PublishActivityStateChangedAsync(session.Code, finalActivity, cancellationToken);
            }

            return Ok(Wrap(summaryText));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateAiSummary failed for session {Code} activity {ActivityId}", code, activityId);
            return StatusCode(500, Error<string>("summary_failed", "Failed to generate summary."));
        }
    }
}
