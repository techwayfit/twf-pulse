namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class FacilitatorUserRecord
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }
}
