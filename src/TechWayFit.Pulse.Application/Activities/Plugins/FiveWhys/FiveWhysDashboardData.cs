using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.FiveWhys;

/// <summary>
/// A single response summary for a FiveWhys activity.
/// </summary>
/// <param name="ResponseId">The response entity ID.</param>
/// <param name="ParticipantId">The participant who submitted the response.</param>
/// <param name="Payload">The raw JSON payload.</param>
/// <param name="CreatedAt">When the response was submitted.</param>
public sealed record FiveWhysResponseSummary(
    Guid ResponseId,
    Guid ParticipantId,
    string Payload,
    DateTimeOffset CreatedAt);

/// <summary>
/// Dashboard data returned by <see cref="FiveWhysActivityPlugin"/>.
/// Holds all data directly since no dedicated Contracts response type exists for FiveWhys.
/// </summary>
public sealed class FiveWhysDashboardData : IActivityDashboardData
{
    public FiveWhysDashboardData(
        Guid sessionId,
        Guid activityId,
        string activityTitle,
        int totalResponses,
        int participantCount,
        int respondedParticipants,
        DateTimeOffset? lastResponseAt,
        IReadOnlyList<FiveWhysResponseSummary> responses)
    {
        SessionId            = sessionId;
        ActivityId           = activityId;
        ActivityTitle        = activityTitle;
        TotalResponses       = totalResponses;
        ParticipantCount     = participantCount;
        RespondedParticipants = respondedParticipants;
        LastResponseAt       = lastResponseAt;
        Responses            = responses;
    }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             { get; }
    public Guid ActivityId            { get; }
    public string ActivityTitle       { get; }
    public int TotalResponses         { get; }
    public int ParticipantCount       { get; }
    public int RespondedParticipants  { get; }
    public DateTimeOffset? LastResponseAt { get; }

    /// <summary>All FiveWhys response payloads for this activity.</summary>
    public IReadOnlyList<FiveWhysResponseSummary> Responses { get; }
}
