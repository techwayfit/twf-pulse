namespace TechWayFit.Pulse.Domain.Entities;

public sealed class SessionGroup
{
    public SessionGroup(
        Guid id,
        string name,
        string? description,
        int level,
        Guid? parentGroupId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid? facilitatorUserId = null,
        string? icon = null,
        string? color = null)
    {
        if (level < 1 || level > 3)
            throw new ArgumentException("Group level must be between 1 and 3.", nameof(level));

        if (level > 1 && parentGroupId == null)
            throw new ArgumentException($"Level {level} group must have a parent group.", nameof(parentGroupId));

        if (level == 1 && parentGroupId != null)
            throw new ArgumentException("Level 1 group cannot have a parent group.", nameof(parentGroupId));

        Id = id;
        Name = name.Trim();
        Description = description?.Trim();
        Level = level;
        ParentGroupId = parentGroupId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        FacilitatorUserId = facilitatorUserId;
        Icon = icon ?? (level == 1 ? "📁" : level == 2 ? "📂" : "📄");
        Color = color;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public int Level { get; }

    public Guid? ParentGroupId { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// ID of the facilitator user who created this group.
    /// Null for legacy groups created before authentication was implemented.
    /// </summary>
    public Guid? FacilitatorUserId { get; private set; }

    /// <summary>
    /// Icon/emoji representing this group (e.g., 📁, 📂, 🎯)
    /// </summary>
    public string Icon { get; private set; }

    /// <summary>
    /// Optional color for the group (hex code)
    /// </summary>
    public string? Color { get; private set; }

    public void Update(string name, string? description, DateTimeOffset updatedAt, string? icon = null, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        Icon = icon ?? Icon;
        Color = color;
        UpdatedAt = updatedAt;
    }
}