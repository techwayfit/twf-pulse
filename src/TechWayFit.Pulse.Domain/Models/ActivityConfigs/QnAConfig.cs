namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Q&amp;A activity type.
/// Supports anonymous submissions, upvoting, and optional moderation.
/// </summary>
public sealed class QnAConfig
{
    /// <summary>Allow participants to submit questions anonymously.</summary>
    public bool AllowAnonymous { get; set; } = true;

    /// <summary>Maximum number of questions a single participant can submit.</summary>
    public int MaxQuestionsPerParticipant { get; set; } = 3;

    /// <summary>Allow participants to upvote other questions.</summary>
    public bool AllowUpvoting { get; set; } = true;

    /// <summary>Maximum character length for a question.</summary>
    public int MaxQuestionLength { get; set; } = 300;

    /// <summary>
    /// When true, questions are hidden from participants until the facilitator approves them.
    /// Currently reserved for future use — defaults to false (no moderation).
    /// </summary>
    public bool RequireModeration { get; set; } = false;
}
