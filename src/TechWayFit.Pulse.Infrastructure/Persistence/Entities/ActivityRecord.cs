namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class ActivityRecord
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public int Order { get; set; }

    public int Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public string? ConfigJson { get; set; }

    public int Status { get; set; }

    public DateTimeOffset? OpenedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }
}
