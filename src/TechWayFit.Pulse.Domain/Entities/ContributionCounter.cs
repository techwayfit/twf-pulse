namespace TechWayFit.Pulse.Domain.Entities;

public sealed class ContributionCounter
{
    public ContributionCounter(
        Guid participantId,
        Guid sessionId,
        int totalContributions,
        DateTimeOffset updatedAt)
    {
        ParticipantId = participantId;
        SessionId = sessionId;
        TotalContributions = totalContributions;
        UpdatedAt = updatedAt;
    }

    public Guid ParticipantId { get; }

    public Guid SessionId { get; }

    public int TotalContributions { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Increment(DateTimeOffset updatedAt)
    {
        TotalContributions += 1;
        UpdatedAt = updatedAt;
    }
}
