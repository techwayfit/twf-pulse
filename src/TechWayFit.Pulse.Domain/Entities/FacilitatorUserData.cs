namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Stores key-value configuration data for facilitator users.
/// Allows flexible storage of user preferences and settings without schema changes.
/// </summary>
public sealed class FacilitatorUserData
{
    public FacilitatorUserData(
        Guid id,
        Guid facilitatorUserId,
        string key,
        string value,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null)
    {
        Id = id;
        FacilitatorUserId = facilitatorUserId;
        Key = key?.Trim() ?? throw new ArgumentNullException(nameof(key));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt ?? createdAt;
        
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }
    }

    public Guid Id { get; }
    
    public Guid FacilitatorUserId { get; }
    
    /// <summary>
    /// Configuration key (e.g., "OpenAI.ApiKey", "OpenAI.BaseUrl")
    /// </summary>
    public string Key { get; private set; }
    
    /// <summary>
    /// Configuration value (should be encrypted for sensitive data)
    /// </summary>
    public string Value { get; private set; }
    
    public DateTimeOffset CreatedAt { get; }
    
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateValue(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Well-known configuration keys for facilitator user data
/// </summary>
public static class FacilitatorUserDataKeys
{
    // AI Configuration
    public const string OpenAiApiKey = "OpenAI.ApiKey";
    public const string OpenAiBaseUrl = "OpenAI.BaseUrl";
    public const string OpenAiModel = "OpenAI.Model";
    
    // AI Quota Tracking
    public const string AiQuotaUsedSessions = "AI.Quota.UsedSessions";
    public const string AiQuotaResetDate = "AI.Quota.ResetDate";
    public const string AiQuotaTier = "AI.Quota.Tier"; // "Free", "BYOK", "Premium"
    
    // UI Preferences
    public const string PreferredLanguage = "UI.PreferredLanguage";
    public const string Theme = "UI.Theme";
}
