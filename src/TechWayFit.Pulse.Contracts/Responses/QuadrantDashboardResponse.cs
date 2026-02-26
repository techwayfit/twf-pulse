namespace TechWayFit.Pulse.Contracts.Responses;

/// <summary>
/// Aggregated dashboard data for the Quadrant (Item Scoring) activity.
/// </summary>
public sealed record QuadrantDashboardResponse(
    Guid SessionId,
    Guid ActivityId,
    string ActivityTitle,
    string XAxisLabel,
    string YAxisLabel,
    int TotalResponses,
    int ParticipantCount,
    int RespondedParticipants,
    IReadOnlyList<QuadrantItemAggregate> Items,
    DateTimeOffset? LastResponseAt);

/// <summary>
/// Per-item aggregate of participant scores.
/// </summary>
public sealed record QuadrantItemAggregate(
    int ItemIndex,
    string ItemLabel,
    double AverageX,
    double AverageY,
    int ResponseCount,
    IReadOnlyList<QuadrantItemNote> Notes);

/// <summary>
/// A note left by a participant alongside their score.
/// </summary>
public sealed record QuadrantItemNote(
    string Note,
    DateTimeOffset CreatedAt);
