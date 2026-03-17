using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
public sealed class SessionsController : SessionApiControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IAuthenticationService _authService;
    private readonly ISessionCodeGenerator _codeGenerator;
    private readonly ISessionGroupService _sessionGroups;
    private readonly TechWayFit.Pulse.Web.Services.IHubNotificationService _hubNotifications;

    public SessionsController(
        ISessionService sessions,
        IAuthenticationService authService,
        IFacilitatorTokenStore facilitatorTokens,
        ISessionCodeGenerator codeGenerator,
        ISessionGroupService sessionGroups,
        TechWayFit.Pulse.Web.Services.IHubNotificationService hubNotifications)
        : base(facilitatorTokens: facilitatorTokens)
    {
        _sessions = sessions;
        _authService = authService;
        _codeGenerator = codeGenerator;
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
            ValidateJoinFormSchema(request.JoinFormSchema);
            var code = await _codeGenerator.GenerateUniqueCodeAsync(cancellationToken);

            var settings = ApiMapper.ToDomain(request.Settings);
            var joinFormSchema = ApiMapper.ToDomain(request.JoinFormSchema);
            var facilitatorUserId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);

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

    [HttpPost("{code}/copy")]
    public async Task<ActionResult<ApiResponse<CreateSessionResponse>>> CopySession(
        string code,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (session is null)
            {
                return NotFound(Error<CreateSessionResponse>("not_found", "Session not found."));
            }

            var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (userId == null || session.FacilitatorUserId != userId)
            {
                return Unauthorized(Error<CreateSessionResponse>("unauthorized", "Only the session owner can copy this session."));
            }

            var newCode = await _codeGenerator.GenerateUniqueCodeAsync(cancellationToken);
            var newSession = await _sessions.CopySessionAsync(session.Id, newCode, DateTimeOffset.UtcNow, cancellationToken);

            return Ok(Wrap(new CreateSessionResponse(newSession.Id, newSession.Code)));
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

            await _sessions.UpdateSessionAsync(
                sessionId,
                request.Title,
                request.Goal,
                request.Context,
                DateTimeOffset.UtcNow,
                cancellationToken);

            var currentSessionForGroup = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (currentSessionForGroup is not null && request.GroupId != currentSessionForGroup.GroupId)
            {
                await _sessions.SetSessionGroupAsync(
                    sessionId,
                    request.GroupId,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
            }

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

            if (request.AllowAnonymous.HasValue || request.StrictCurrentActivityOnly.HasValue)
            {
                var currentSessionForSettings = await _sessions.GetByCodeAsync(code, cancellationToken);
                if (currentSessionForSettings is not null)
                {
                    var settings = new Domain.ValueObjects.SessionSettings(
                        strictCurrentActivityOnly: request.StrictCurrentActivityOnly ?? currentSessionForSettings.Settings.StrictCurrentActivityOnly,
                        allowAnonymous: request.AllowAnonymous ?? currentSessionForSettings.Settings.AllowAnonymous,
                        ttlMinutes: request.TtlMinutes ?? currentSessionForSettings.Settings.TtlMinutes);

                    await _sessions.UpdateSessionSettingsAsync(
                        sessionId,
                        settings,
                        DateTimeOffset.UtcNow,
                        cancellationToken);
                }
            }

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
}
