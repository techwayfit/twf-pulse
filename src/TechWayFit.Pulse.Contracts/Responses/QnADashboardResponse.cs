namespace TechWayFit.Pulse.Contracts.Responses;

/// <summary>Aggregated dashboard data for a Q&amp;A activity.</summary>
public sealed record QnADashboardResponse(
    Guid SessionId,
    Guid ActivityId,
    string ActivityTitle,
    int TotalQuestions,
    int TotalVotes,
    int ParticipantCount,
    int RespondedParticipants,
    IReadOnlyList<QnAQuestionItem> Questions,
    DateTimeOffset? LastResponseAt);

/// <summary>A single question with its aggregated upvote count.</summary>
public sealed record QnAQuestionItem(
    /// <summary>The Response entity ID — used as the key for upvoting.</summary>
    Guid ResponseId,
    string Text,
    int UpvoteCount,
    bool IsAnonymous,
    bool IsAnswered,
    DateTimeOffset SubmittedAt,
    /// <summary>
    /// The participant who submitted this question.
    /// Included for server-side logic only (e.g., preventing self-votes in Blazor Server).
    /// </summary>
    Guid SubmittedByParticipantId);
