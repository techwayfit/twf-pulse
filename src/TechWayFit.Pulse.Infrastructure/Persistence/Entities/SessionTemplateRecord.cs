namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class SessionTemplateRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Category { get; set; }

    public string IconEmoji { get; set; } = string.Empty;

    public string ConfigJson { get; set; } = string.Empty;

    public bool IsSystemTemplate { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
