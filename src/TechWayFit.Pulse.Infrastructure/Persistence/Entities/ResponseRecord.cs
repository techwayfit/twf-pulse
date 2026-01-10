namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class ResponseRecord
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public Guid ActivityId { get; set; }

    public Guid ParticipantId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public string DimensionsJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
