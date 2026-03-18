using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Commands;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Hubs;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
public sealed class ParticipantsController : SessionApiControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IActivityService _activities;
    private readonly IParticipantService _participants;
    private readonly IResponseService _responses;
    private readonly IAuthenticationService _authService;
    private readonly IFacilitatorTokenStore _facilitatorTokens;
    private readonly IParticipantTokenStore _participantTokens;
    private readonly IHubContext<WorkshopHub, IWorkshopClient> _hub;

    public ParticipantsController(
        ISessionService sessions,
        IActivityService activities,
        IParticipantService participants,
        IResponseService responses,
        IAuthenticationService authService,
        IFacilitatorTokenStore facilitatorTokens,
        IParticipantTokenStore participantTokens,
        IHubContext<WorkshopHub, IWorkshopClient> hub)
        : base(facilitatorTokens: facilitatorTokens, participantTokens: participantTokens)
    {
        _sessions = sessions;
        _activities = activities;
        _participants = participants;
        _responses = responses;
        _authService = authService;
        _facilitatorTokens = facilitatorTokens;
        _participantTokens = participantTokens;
        _hub = hub;
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

        var activities = await _activities.GetAgendaAsync(session.Id, cancellationToken);
        var activity = activities.FirstOrDefault(a => a.Id == activityId);
        if (activity is null)
        {
            return NotFound(Error<int>("not_found", "Activity not found for this session."));
        }

        var responses = await _responses.GetByParticipantAsync(session.Id, participantId, cancellationToken);
        var count = responses.Count(r => r.ActivityId == activityId);

        return Ok(Wrap(count));
    }

    [HttpPost("{code}/facilitators/join")]
    [EnableRateLimiting("api-write")]
    public async Task<ActionResult<ApiResponse<JoinFacilitatorResponse>>> JoinFacilitator(
        string code,
        [FromBody] JoinFacilitatorRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;

        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<JoinFacilitatorResponse>("not_found", "Session not found."));
        }

        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null || session.FacilitatorUserId != userId)
        {
            return Unauthorized(Error<JoinFacilitatorResponse>("unauthorized", "Only the session owner can join as facilitator."));
        }

        var auth = _facilitatorTokens.Create(session.Id);
        return Ok(Wrap(new JoinFacilitatorResponse(auth.FacilitatorId, auth.Token)));
    }

    [HttpPost("{code}/participants/join")]
    [EnableRateLimiting("participant-join")]
    public async Task<ActionResult<ApiResponse<JoinParticipantResponse>>> JoinParticipant(
        string code,
        [FromBody] JoinParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByCodeAsync(code, cancellationToken);
        if (session is null)
        {
            return NotFound(Error<JoinParticipantResponse>("not_found", "Session not found."));
        }

        var joinResult = await _participants.JoinAsync(
            new JoinParticipantCommand(
                session.Id,
                request.DisplayName,
                request.IsAnonymous,
                request.Dimensions ?? new Dictionary<string, string?>(),
                DateTimeOffset.UtcNow),
            cancellationToken);
        if (!joinResult.IsSuccess || joinResult.Value is null)
        {
            return FromResult<JoinParticipantResponse>(joinResult);
        }

        var participant = joinResult.Value;
        var participantCount = await _participants.GetBySessionAsync(session.Id, cancellationToken);

        await _hub.Clients.Group(WorkshopGroupNames.ForSession(session.Code)).ParticipantJoined(new ParticipantJoinedEvent(
            session.Code,
            participant.Id,
            participant.DisplayName,
            participantCount.Count,
            DateTimeOffset.UtcNow));

        if (!string.IsNullOrEmpty(participant.Token))
        {
            await _participantTokens.TryGetAsync(session.Id, participant.Id);
        }

        return Ok(Wrap(new JoinParticipantResponse(
            participant.Id,
            participant.Token ?? throw new InvalidOperationException("Participant token not generated"))));
    }
}
