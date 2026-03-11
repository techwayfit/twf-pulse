namespace TechWayFit.Pulse.Web.Models;

public class PersonalizedDashboardViewModel
{
    public string UserDisplayName { get; set; } = string.Empty;
    public int TotalGroups { get; set; }
    public int ActiveSessionsCount { get; set; }
    public PersonalizedSessionCardViewModel SessionCard { get; set; } = new();
}

/// <summary>
/// Represents the personalized session card state for the user.
/// Shows different content based on user's current session status.
/// </summary>
public class PersonalizedSessionCardViewModel
{
    /// <summary>
    /// The state of the user's sessions: NoSessions, ActiveSession, UpcomingSession, or RecentlyCompleted
    /// </summary>
    public SessionCardState State { get; set; } = SessionCardState.NoSessions;
    
    /// <summary>
    /// Session code (for active/upcoming sessions)
    /// </summary>
    public string? SessionCode { get; set; }
    
    /// <summary>
    /// Session title
    /// </summary>
    public string? SessionTitle { get; set; }
    
    /// <summary>
 /// Session ID (for reports/navigation)
    /// </summary>
    public Guid? SessionId { get; set; }
    
    /// <summary>
 /// Number of participants (for active sessions)
    /// </summary>
    public int ParticipantCount { get; set; }
    
    /// <summary>
    /// Session start time (for upcoming sessions)
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
  /// Session end time (for recently completed sessions)
    /// </summary>
  public DateTime? EndTime { get; set; }
}

/// <summary>
/// Defines the possible states for the personalized session card
/// </summary>
public enum SessionCardState
{
    /// <summary>
    /// User has no sessions - show "Create Session" CTA
    /// </summary>
    NoSessions,
    
    /// <summary>
    /// User has an active session right now - show "Go to Session" link
    /// </summary>
    ActiveSession,
    
    /// <summary>
    /// User has an upcoming session scheduled - show "View Upcoming" link
    /// </summary>
    UpcomingSession,
    
    /// <summary>
  /// User recently completed a session - show "View Report" link
    /// </summary>
    RecentlyCompleted
}
