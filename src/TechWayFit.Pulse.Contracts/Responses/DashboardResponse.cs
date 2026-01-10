namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record DashboardResponse(
    Guid SessionId,
    Guid? ActivityId,
    int TotalResponses,
    int ParticipantCount,
    int RespondedParticipants,
    IReadOnlyList<WordCloudItem> WordCloud,
    IReadOnlyList<QuadrantPoint> QuadrantPoints);
