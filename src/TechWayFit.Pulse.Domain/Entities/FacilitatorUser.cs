namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Represents a facilitator user who can create and manage workshop sessions.
/// </summary>
public sealed class FacilitatorUser
{
    public FacilitatorUser(
        Guid id,
        string email,
        string displayName,
     DateTimeOffset createdAt,
        DateTimeOffset? lastLoginAt = null)
    {
        Id = id;
        Email = email.Trim().ToLowerInvariant();
 DisplayName = displayName.Trim();
   CreatedAt = createdAt;
        LastLoginAt = lastLoginAt;
    }

    public Guid Id { get; }

    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public void UpdateLastLogin(DateTimeOffset loginAt)
    {
        LastLoginAt = loginAt;
    }

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
    }
}
