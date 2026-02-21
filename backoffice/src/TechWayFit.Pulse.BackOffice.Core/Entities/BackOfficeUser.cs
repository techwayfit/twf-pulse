namespace TechWayFit.Pulse.BackOffice.Core.Entities;

/// <summary>
/// Represents an operator who can access the BackOffice portal.
/// Stored separately from FacilitatorUser — BackOffice users are internal operators only.
/// </summary>
public sealed class BackOfficeUser
{
    public BackOfficeUser(
        Guid id,
        string username,
        string passwordHash,
        string role,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset? lastLoginAt = null)
    {
        Id = id;
        Username = username.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role.Trim();
        IsActive = isActive;
        CreatedAt = createdAt;
        LastLoginAt = lastLoginAt;
    }

    public Guid Id { get; }

    public string Username { get; private set; }

    /// <summary>BCrypt password hash. Never store plaintext.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>"Operator" or "SuperAdmin"</summary>
    public string Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public void UpdateLastLogin(DateTimeOffset loginAt) => LastLoginAt = loginAt;

    public void UpdateRole(string role) => Role = role.Trim();

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
