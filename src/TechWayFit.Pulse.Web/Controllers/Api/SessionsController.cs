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
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IActivityService _activities;
    private readonly IParticipantService _participants;
    private readonly IResponseService _responses;
    private readonly IDashboardService _dashboards;
    private readonly IFacilitatorTokenStore _facilitatorTokens;
    private readonly IHubContext<WorkshopHub, IWorkshopClient> _hub;

    public SessionsController(
        ISessionService sessions,
        IActivityService activities,
        IParticipantService participants,
        IResponseService responses,
      IDashboardService dashboards,
        IFacilitatorTokenStore facilitatorTokens,
        IHubContext<WorkshopHub, IWorkshopClient> hub)
    {
        _sessions = sessions;
        _activities = activities;
     _participants = participants;
   _responses = responses;
        _dashboards = dashboards;
        _facilitatorTokens = facilitatorTokens;
        _hub = hub;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateSessionResponse>>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
     try
     {
  var settings = ApiMapper.ToDomain(request.Settings);
      var joinFormSchema = ApiMapper.ToDomain(request.JoinFormSchema);
          var session = await _sessions.CreateSessionAsync(
  request.Code,
 request.Title,
            request.Goal,
    request.Context,
           settings,
     joinFormSchema,
            DateTimeOffset.UtcNow,
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

    [HttpPost("{code}/facilitators/join")]
    public async Task<ActionResult<ApiResponse<JoinFacilitatorResponse>>> JoinFacilitator(
        string code,
  [FromBody] JoinFacilitatorRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
   return NotFound(Error<JoinFacilitatorResponse>("not_found", "Session not found."));
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
            await PublishSessionStateChangedAsync(updated, cancellationToken);
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
            await PublishSessionStateChangedAsync(updated, cancellationToken);
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

    var activity = await _activities.AddActivityAsync(
        session.Id,
                request.Order,
    ApiMapper.MapActivityType(request.Type),
              request.Title,
    request.Prompt,
    request.Config,
        cancellationToken);

          await PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
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
       await PublishSessionStateChangedAsync(updated, cancellationToken);
          }

       var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
         var activity = agenda.FirstOrDefault(item => item.Id == activityId);
            if (activity is not null)
      {
  await PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
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
await PublishSessionStateChangedAsync(updated, cancellationToken);
  }

     var agenda = await _activities.GetAgendaAsync(session.Id, cancellationToken);
        var activity = agenda.FirstOrDefault(item => item.Id == activityId);
       if (activity is not null)
            {
  await PublishActivityStateChangedAsync(session.Code, activity, cancellationToken);
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

            return Ok(Wrap(new JoinParticipantResponse(participant.Id)));
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

    private static ApiResponse<T> Wrap<T>(T data)
    {
        return new ApiResponse<T>(data);
    }

    private static ApiResponse<T> Error<T>(string code, string message)
 {
        return new ApiResponse<T>(default, new[] { new ApiError(code, message) });
    }

    private const string FacilitatorTokenHeader = "X-Facilitator-Token";

  private ActionResult<ApiResponse<T>>? RequireFacilitatorToken<T>(TechWayFit.Pulse.Domain.Entities.Session session)
    {
        if (!_facilitatorTokens.TryGet(session.Id, out var auth))
        {
            return null;
        }

        if (Request.Headers.TryGetValue(FacilitatorTokenHeader, out var token)
     && string.Equals(token.ToString(), auth.Token, StringComparison.Ordinal))
        {
   return null;
        }

   return Unauthorized(Error<T>("facilitator_token_required", "Facilitator token is required."));
 }

    /// <summary>
    /// Broadcast session state change event to all session participants
    /// </summary>
    private async Task PublishSessionStateChangedAsync(
        TechWayFit.Pulse.Domain.Entities.Session session,
    CancellationToken cancellationToken)
    {
        var participants = await _participants.GetBySessionAsync(session.Id, cancellationToken);
        
        var sessionStateEvent = new SessionStateChangedEvent(
    session.Code,
 ApiMapper.MapSessionStatus(session.Status),
   session.CurrentActivityId,
 participants.Count,
            DateTimeOffset.UtcNow);

        await _hub.Clients.Group(session.Code).SessionStateChanged(sessionStateEvent);
    }

  /// <summary>
    /// Broadcast activity state change event to all session participants
    /// </summary>
    private async Task PublishActivityStateChangedAsync(
        string sessionCode,
        TechWayFit.Pulse.Domain.Entities.Activity activity,
        CancellationToken cancellationToken)
    {
        var activityStateEvent = new ActivityStateChangedEvent(
            sessionCode,
   activity.Id,
        activity.Order,
    activity.Title,
            ApiMapper.MapActivityStatus(activity.Status),
       activity.OpenedAt,
            activity.ClosedAt,
  DateTimeOffset.UtcNow);

      await _hub.Clients.Group(sessionCode).ActivityStateChanged(activityStateEvent);
    }
}
