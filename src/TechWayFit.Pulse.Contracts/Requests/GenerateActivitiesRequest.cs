namespace TechWayFit.Pulse.Contracts.Requests;

/// <summary>
/// Request to generate AI activities for an existing session
/// </summary>
public sealed record GenerateActivitiesRequest
{
    /// <summary>
    /// Optional additional context about the session, team, or specific topics
    /// </summary>
    public string? AdditionalContext { get; init; }

    /// <summary>
    /// Workshop type hint for AI generation (e.g., retrospective, ops, discovery)
    /// </summary>
    public string? WorkshopType { get; init; }

    /// <summary>
    /// Target number of activities to generate (defaults to 4)
    /// </summary>
    public int? TargetActivityCount { get; init; }

    /// <summary>
    /// Session duration in minutes
    /// </summary>
    public int? DurationMinutes { get; init; }

    /// <summary>
    /// Expected number of participants
    /// </summary>
    public int? ParticipantCount { get; init; }

    /// <summary>
    /// Type of participants (e.g., "developers", "product managers", "mixed team")
    /// </summary>
    public string? ParticipantType { get; init; }
}
