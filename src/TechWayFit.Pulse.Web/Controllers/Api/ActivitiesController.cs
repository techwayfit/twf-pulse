using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Hubs;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
public sealed class ActivitiesController : SessionApiControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IActivityService _activities;
    private readonly IHubContext<WorkshopHub, IWorkshopClient> _hub;
    private readonly TechWayFit.Pulse.Web.Services.IHubNotificationService _hubNotifications;
    private readonly ISessionActivityMetadataService _metadataService;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(
        ISessionService sessions,
        IActivityService activities,
        IFacilitatorTokenStore facilitatorTokens,
        IHubContext<WorkshopHub, IWorkshopClient> hub,
        TechWayFit.Pulse.Web.Services.IHubNotificationService hubNotifications,
        ISessionActivityMetadataService metadataService,
        ILogger<ActivitiesController> logger)
        : base(facilitatorTokens: facilitatorTokens)
    {
        _sessions = sessions;
        _activities = activities;
        _hub = hub;
        _hubNotifications = hubNotifications;
        _metadataService = metadataService;
        _logger = logger;
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

    [HttpPost("{code}/activities/bulk")]
    public async Task<ActionResult<ApiResponse<BulkCreateActivitiesResponse>>> BulkCreateActivities(
        string code,
        [FromBody] BulkCreateActivitiesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<BulkCreateActivitiesResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<BulkCreateActivitiesResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            if (request.Activities == null || request.Activities.Count == 0)
            {
                return BadRequest(Error<BulkCreateActivitiesResponse>("validation_error", "No activities provided."));
            }

            if (request.Activities.Count > 100)
            {
                return BadRequest(Error<BulkCreateActivitiesResponse>("validation_error", "Cannot create more than 100 activities at once."));
            }

            var createdActivityIds = new List<Guid>();
            var errors = new List<string>();

            var existingActivities = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var nextOrder = existingActivities.Count + 1;

            foreach (var item in request.Activities.OrderBy(a => a.Order))
            {
                try
                {
                    var activity = await _activities.AddActivityAsync(
                        session.Id,
                        nextOrder++,
                        ApiMapper.MapActivityType(item.Type),
                        item.Title,
                        item.Prompt,
                        item.Config,
                        item.DurationMinutes,
                        cancellationToken);

                    createdActivityIds.Add(activity.Id);
                    await _hubNotifications.PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {item.Order}: {ex.Message}");
                }
            }

            var response = new BulkCreateActivitiesResponse(
                createdActivityIds.Count,
                createdActivityIds,
                errors.Any() ? errors : null);

            if (createdActivityIds.Count == 0)
            {
                return BadRequest(Error<BulkCreateActivitiesResponse>("bulk_create_failed", "Failed to create any activities. Check errors."));
            }

            return Ok(Wrap(response));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<BulkCreateActivitiesResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<BulkCreateActivitiesResponse>("validation_error", ex.Message));
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

            await _activities.DeleteActivityAsync(session.Id, activityId, cancellationToken);

            await _hub.Clients.Group(WorkshopGroupNames.ForSession(session.Code)).ActivityDeleted(activityId);
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

    [HttpPost("{code}/activities/{activityId:guid}/copy")]
    public async Task<ActionResult<ApiResponse<AgendaActivityResponse>>> CopyActivity(
        string code,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<AgendaActivityResponse>("not_found", "Session not found."));
            }

            var authError = RequireFacilitatorToken<AgendaActivityResponse>(session);
            if (authError is not null)
            {
                return authError;
            }

            var copiedActivity = await _activities.CopyActivityAsync(session.Id, activityId, cancellationToken);

            await _hubNotifications.PublishActivityStateChangedAsync(session.Code, copiedActivity, cancellationToken);
            return Ok(Wrap(ApiMapper.ToAgenda(copiedActivity)));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<AgendaActivityResponse>("validation_error", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Error<AgendaActivityResponse>("validation_error", ex.Message));
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

    [HttpPost("{code}/activities/{activityId:guid}/quadrant/set-item")]
    public async Task<ActionResult<ApiResponse<object>>> SetQuadrantItem(
        string code,
        Guid activityId,
        [FromBody] SetQuadrantItemRequest request,
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

            var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
            var activity = agenda.FirstOrDefault(a => a.Id == activityId);
            if (activity is null)
            {
                return NotFound(Error<object>("not_found", "Activity not found."));
            }

            if (activity.Type != TechWayFit.Pulse.Domain.Enums.ActivityType.Quadrant)
            {
                return BadRequest(Error<object>("invalid_type", "Only Quadrant activities support set-item."));
            }

            if (request.ItemIndex < 0)
            {
                return BadRequest(Error<object>("validation_error", "ItemIndex must be >= 0."));
            }

            await _hubNotifications.PublishQuadrantItemAdvancedAsync(
                session.Code,
                activityId,
                request.ItemIndex,
                cancellationToken);

            await _metadataService.SetValueAsync(
                session.Id,
                activityId,
                TechWayFit.Pulse.Domain.Models.ActivityMetadataKeys.QuadrantCurrentItemIndex,
                request.ItemIndex.ToString(),
                cancellationToken);

            return Ok(Wrap<object>(new { itemIndex = request.ItemIndex }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error<object>("validation_error", ex.Message));
        }
    }

    [HttpGet("{code}/activities/{activityId:guid}/metadata/{key}")]
    public async Task<ActionResult<ApiResponse<string?>>> GetActivityMetadata(
        string code,
        Guid activityId,
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<string?>("not_found", "Session not found."));
            }

            var value = await _metadataService.GetValueAsync(session.Id, activityId, key, cancellationToken);
            return Ok(Wrap<string?>(value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for activity {ActivityId} key {Key}", activityId, key);
            return StatusCode(500, Error<string?>("internal_error", "Failed to retrieve metadata."));
        }
    }
}
