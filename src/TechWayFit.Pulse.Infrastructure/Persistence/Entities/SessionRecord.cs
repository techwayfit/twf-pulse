namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class SessionRecord
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public string? ContextJson { get; set; }

    public string SettingsJson { get; set; } = string.Empty;

    public string JoinFormSchemaJson { get; set; } = string.Empty;

    public int Status { get; set; }

    public Guid? CurrentActivityId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public Guid? FacilitatorUserId { get; set; }
}
