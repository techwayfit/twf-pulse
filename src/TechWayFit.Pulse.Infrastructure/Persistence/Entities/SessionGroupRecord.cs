namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class SessionGroupRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Level { get; set; }

    public Guid? ParentGroupId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? FacilitatorUserId { get; set; }
}