namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class LoginOtpRecord
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string OtpCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTimeOffset? UsedAt { get; set; }
}
