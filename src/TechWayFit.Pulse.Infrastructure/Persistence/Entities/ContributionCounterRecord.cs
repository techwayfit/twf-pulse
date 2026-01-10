namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class ContributionCounterRecord
{
    public Guid ParticipantId { get; set; }

    public Guid SessionId { get; set; }

    public int TotalContributions { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
