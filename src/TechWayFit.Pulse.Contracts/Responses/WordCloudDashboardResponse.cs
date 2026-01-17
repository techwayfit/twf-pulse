namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record WordCloudDashboardResponse(
    Guid SessionId,
    Guid ActivityId,
    string ActivityTitle,
    int TotalResponses,
    int ParticipantCount,
    int RespondedParticipants,
    IReadOnlyList<WordCloudItem> WordFrequencies,
    DateTimeOffset? LastResponseAt,
    int TotalWords,
    int UniqueWords);