using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
public sealed class DashboardsController : SessionApiControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IParticipantService _participants;
    private readonly IResponseService _responses;
    private readonly IDashboardService _dashboards;
    private readonly IPollDashboardService _pollDashboards;
    private readonly IWordCloudDashboardService _wordCloudDashboards;
    private readonly IRatingDashboardService _ratingDashboards;
    private readonly IGeneralFeedbackDashboardService _generalFeedbackDashboards;

    public DashboardsController(
        ISessionService sessions,
        IParticipantService participants,
        IResponseService responses,
        IDashboardService dashboards,
        IPollDashboardService pollDashboards,
        IWordCloudDashboardService wordCloudDashboards,
        IRatingDashboardService ratingDashboards,
        IGeneralFeedbackDashboardService generalFeedbackDashboards)
    {
        _sessions = sessions;
        _participants = participants;
        _responses = responses;
        _dashboards = dashboards;
        _pollDashboards = pollDashboards;
        _wordCloudDashboards = wordCloudDashboards;
        _ratingDashboards = ratingDashboards;
        _generalFeedbackDashboards = generalFeedbackDashboards;
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
}
