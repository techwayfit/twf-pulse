namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record RatingDashboardResponse(
    Guid SessionId,
    Guid ActivityId,
    string ActivityTitle,
    int TotalResponses,
    int ParticipantCount,
    int RespondedParticipants,
    double AverageRating,
    double MedianRating,
    int MinRating,
    int MaxRating,
    IReadOnlyList<RatingDistributionItem> Distribution,
    IReadOnlyList<RatingCommentItem> Comments,
    DateTimeOffset? LastResponseAt);

public sealed record RatingDistributionItem(
    int Rating,
    int Count,
    double Percentage);

public sealed record RatingCommentItem(
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);
