namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class ParticipantRecord
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string? DisplayName { get; set; }

    public bool IsAnonymous { get; set; }

    public string DimensionsJson { get; set; } = string.Empty;

    public string? Token { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}
