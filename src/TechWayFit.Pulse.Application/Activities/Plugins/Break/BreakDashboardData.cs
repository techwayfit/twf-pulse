using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Break;

/// <summary>
/// Dashboard data returned by <see cref="BreakActivityPlugin"/>.
/// Holds data directly since Break does not accept participant responses
/// and has no dedicated Contracts response type.
/// </summary>
public sealed class BreakDashboardData : IActivityDashboardData
{
    public BreakDashboardData(
        Guid sessionId,
        Guid activityId,
        string activityTitle,
        int participantCount)
    {
        SessionId        = sessionId;
        ActivityId       = activityId;
        ActivityTitle    = activityTitle;
        ParticipantCount = participantCount;
    }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             { get; }
    public Guid ActivityId            { get; }
    public string ActivityTitle       { get; }
    public int TotalResponses         => 0;
    public int ParticipantCount       { get; }
    public int RespondedParticipants  => 0;
    public DateTimeOffset? LastResponseAt => null;
}
