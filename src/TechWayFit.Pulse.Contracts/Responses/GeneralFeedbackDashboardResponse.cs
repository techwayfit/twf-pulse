namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record GeneralFeedbackDashboardResponse(
    Guid SessionId,
    Guid ActivityId,
    string ActivityTitle,
    int TotalResponses,
    int ParticipantCount,
    int RespondedParticipants,
    IReadOnlyList<FeedbackItem> Feedbacks,
    IReadOnlyList<string> TopKeywords,
    DateTimeOffset? LastResponseAt,
    int AverageWordCount);

public sealed record FeedbackItem(
    string Content,
    int WordCount,
    string? Category,
    DateTimeOffset CreatedAt,
    bool IsAnonymous);
