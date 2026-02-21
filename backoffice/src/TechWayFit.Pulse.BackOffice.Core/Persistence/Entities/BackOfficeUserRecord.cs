namespace TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;

public sealed class BackOfficeUserRecord
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>"Operator" or "SuperAdmin"</summary>
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
