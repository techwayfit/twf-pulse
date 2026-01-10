namespace TechWayFit.Pulse.Contracts.Responses;

public sealed record ParticipantDashboardResponse(
    Guid SessionId,
    Guid ParticipantId,
    int TotalResponses,
    int DistinctActivities,
    DateTimeOffset? LastResponseAt);
