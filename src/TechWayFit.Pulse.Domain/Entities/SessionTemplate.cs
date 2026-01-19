using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Represents a reusable session template with pre-configured activities
/// </summary>
public sealed class SessionTemplate
{
    public SessionTemplate(
        Guid id,
        string name,
        string description,
        TemplateCategory category,
        string iconEmoji,
        string configJson,
        bool isSystemTemplate,
        Guid? createdByUserId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name.Trim();
        Description = description.Trim();
        Category = category;
        IconEmoji = iconEmoji;
        ConfigJson = configJson;
        IsSystemTemplate = isSystemTemplate;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public TemplateCategory Category { get; private set; }

    public string IconEmoji { get; private set; }

    /// <summary>
    /// JSON configuration for the template (includes session settings and activities)
    /// </summary>
    public string ConfigJson { get; private set; }

    /// <summary>
    /// True if this is a built-in template, false if user-created
    /// </summary>
    public bool IsSystemTemplate { get; }

    /// <summary>
    /// User who created this template (null for system templates)
    /// </summary>
    public Guid? CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string name, string description, TemplateCategory category, string iconEmoji, string configJson)
    {
        Name = name.Trim();
        Description = description.Trim();
        Category = category;
        IconEmoji = iconEmoji;
        ConfigJson = configJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
