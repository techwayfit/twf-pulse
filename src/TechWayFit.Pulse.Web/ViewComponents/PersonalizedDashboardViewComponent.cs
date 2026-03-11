using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.DTOs;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Models;

namespace TechWayFit.Pulse.Web.ViewComponents;

/// <summary>
/// Displays personalized dashboard widgets for logged-in facilitators on the home page.
/// Shows recent sessions, quick stats, and quick actions.
/// OPTIMIZED: Uses lightweight SessionSummaryDto instead of full Session entities.
/// </summary>
public class PersonalizedDashboardViewComponent : ViewComponent
{
    private readonly IAuthenticationService _authService;
    private readonly ISessionGroupService _sessionGroups;
    private readonly ISessionRepository _sessionRepository;
    private readonly IParticipantService _participantService;

    public PersonalizedDashboardViewComponent(
        IAuthenticationService authService,
        ISessionGroupService sessionGroups,
        ISessionRepository sessionRepository,
        IParticipantService participantService)
    {
        _authService = authService;
        _sessionGroups = sessionGroups;
        _sessionRepository = sessionRepository;
        _participantService = participantService;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        // Only show for authenticated users
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Content(string.Empty);
        }

        // Cast to ClaimsPrincipal to access claims
        var claimsPrincipal = User as ClaimsPrincipal;
        if (claimsPrincipal == null)
        {
            return Content(string.Empty);
        }

        // Get facilitator user ID from claims
        Guid? userId = null;
        var userIdClaim = claimsPrincipal.FindFirst("FacilitatorUserId")?.Value;
        if (Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            var facilitator = await _authService.GetFacilitatorAsync(parsedUserId, cancellationToken);
            if (facilitator != null)
            {
                userId = facilitator.Id;
            }
        }

        if (userId == null)
        {
            var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var facilitator = await _authService.GetFacilitatorByEmailAsync(email, cancellationToken);
                userId = facilitator?.Id;
            }
        }

        if (userId == null)
        {
            return Content(string.Empty);
        }

        // Get groups count
        var groups = await _sessionGroups.GetFacilitatorGroupsAsync(userId.Value, cancellationToken);

        // OPTIMIZED: Use lightweight session summaries instead of full entities
        // This reduces memory and network by ~70% (no Settings, JoinFormSchema, etc.)
        var sessionSummaries = await _sessionRepository.GetSessionSummariesByFacilitatorAsync(userId.Value, cancellationToken);

        // Calculate active sessions count (Live status)
        var activeSessionsCount = sessionSummaries.Count(s => s.Status == SessionStatus.Live);

        // Determine the personalized session card state
        var sessionCard = await DetermineSessionCardStateAsync(sessionSummaries, cancellationToken);

        var model = new PersonalizedDashboardViewModel
        {
            UserDisplayName = User.Identity?.Name ?? "User",
            TotalGroups = groups.Count,
            ActiveSessionsCount = activeSessionsCount,
            SessionCard = sessionCard
        };

        return View(model);
    }

    /// <summary>
    /// Analyzes user's sessions to determine what to show in the personalized card.
    /// Priority: Active > Upcoming > Recently Completed > No Sessions
    /// OPTIMIZED: Works with lightweight SessionSummaryDto instead of full Session entities.
    /// </summary>
    private async Task<PersonalizedSessionCardViewModel> DetermineSessionCardStateAsync(
        IReadOnlyList<SessionSummaryDto> sessionSummaries,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Priority 1: Active session (Live status)
        var activeSession = sessionSummaries
            .Where(s => s.Status == SessionStatus.Live)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault();

        if (activeSession != null)
        {
            // Only query participant count for the active session we're displaying
            var participantCount = (await _participantService.GetBySessionAsync(activeSession.Id, cancellationToken)).Count;

            return new PersonalizedSessionCardViewModel
            {
                State = SessionCardState.ActiveSession,
                SessionCode = activeSession.Code,
                SessionTitle = activeSession.Title,
                SessionId = activeSession.Id,
                ParticipantCount = participantCount
            };
        }

        // Priority 2: Upcoming session (scheduled in future, status = Draft)
        var upcomingSession = sessionSummaries
            .Where(s => s.Status == SessionStatus.Draft
                         && s.SessionStart.HasValue
                         && s.SessionStart.Value > now)
            .OrderBy(s => s.SessionStart)
            .FirstOrDefault();

        if (upcomingSession != null)
        {
            return new PersonalizedSessionCardViewModel
            {
                State = SessionCardState.UpcomingSession,
                SessionCode = upcomingSession.Code,
                SessionTitle = upcomingSession.Title,
                SessionId = upcomingSession.Id,
                StartTime = upcomingSession.SessionStart
            };
        }

        // Priority 3: Recently completed session (Ended status, within last 24 hours)
        var recentlyCompleted = sessionSummaries
            .Where(s => s.Status == SessionStatus.Ended
                         && s.UpdatedAt >= DateTimeOffset.UtcNow.AddHours(-24))
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault();

        if (recentlyCompleted != null)
        {
            return new PersonalizedSessionCardViewModel
            {
                State = SessionCardState.RecentlyCompleted,
                SessionCode = recentlyCompleted.Code,
                SessionTitle = recentlyCompleted.Title,
                SessionId = recentlyCompleted.Id,
                EndTime = recentlyCompleted.UpdatedAt.DateTime
            };
        }

        // Priority 4: No sessions (show create CTA)
        return new PersonalizedSessionCardViewModel
        {
            State = SessionCardState.NoSessions
        };
    }
}
