using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Hubs;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
public sealed class ResponsesController : SessionApiControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IParticipantService _participants;
    private readonly IResponseService _responses;
    private readonly IHubContext<WorkshopHub, IWorkshopClient> _hub;
    private readonly ILogger<ResponsesController> _logger;

    public ResponsesController(
        ISessionService sessions,
        IParticipantService participants,
        IResponseService responses,
        IParticipantTokenStore participantTokens,
        IHubContext<WorkshopHub, IWorkshopClient> hub,
        ILogger<ResponsesController> logger)
        : base(participantTokens: participantTokens)
    {
        _sessions = sessions;
        _participants = participants;
        _responses = responses;
        _hub = hub;
        _logger = logger;
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

            var tokenValidationResult = await RequireParticipantToken<SubmitResponseResponse>(session.Id, request.ParticipantId);
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

            var sessionGroup = WorkshopGroupNames.ForSession(session.Code);
            _logger.LogInformation(
                "Response submitted: SessionCode={SessionCode}, Group={Group}, ActivityId={ActivityId}, ParticipantId={ParticipantId}, ResponseId={ResponseId}",
                session.Code,
                sessionGroup,
                activityId,
                request.ParticipantId,
                response.Id);

            await _hub.Clients.Group(sessionGroup).ResponseReceived(new ResponseReceivedEvent(
                session.Code,
                activityId,
                response.Id,
                request.ParticipantId,
                response.CreatedAt,
                DateTimeOffset.UtcNow));

            await _hub.Clients.Group(sessionGroup).DashboardUpdated(new DashboardUpdatedEvent(
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
}
