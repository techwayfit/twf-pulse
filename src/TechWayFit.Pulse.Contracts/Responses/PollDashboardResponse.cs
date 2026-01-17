namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record PollDashboardResponse(
    Guid SessionId,
    Guid ActivityId,
    string ActivityTitle,
    int TotalResponses,
    int ParticipantCount,
    int RespondedParticipants,
    IReadOnlyList<PollOptionResult> Results,
    DateTimeOffset? LastResponseAt);

public sealed record PollOptionResult(
    string Id,
    string Label,
    int Count,
    double Percentage);