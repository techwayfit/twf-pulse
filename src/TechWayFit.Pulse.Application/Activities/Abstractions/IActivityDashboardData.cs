namespace TechWayFit.Pulse.Application.Activities.Abstractions;

/// <summary>
/// Marker interface for typed dashboard data returned by activity plugins.
/// Each activity plugin returns its own concrete type (e.g. PollDashboardData).
/// Callers in the Web layer cast to the expected type after looking up the plugin.
/// </summary>
public interface IActivityDashboardData
{
    Guid SessionId { get; }
    Guid ActivityId { get; }
    string ActivityTitle { get; }
    int TotalResponses { get; }
    int ParticipantCount { get; }
    int RespondedParticipants { get; }
    DateTimeOffset? LastResponseAt { get; }
}
