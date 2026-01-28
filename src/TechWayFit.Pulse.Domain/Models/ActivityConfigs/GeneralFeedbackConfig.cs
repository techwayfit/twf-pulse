using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for General Feedback activity type.
/// Supports long-form text with optional categorization.
/// </summary>
public sealed class GeneralFeedbackConfig
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public GeneralFeedbackConfig()
    {
        MaxLength = 1000;
        MinLength = 10;
        Placeholder = "Share your thoughts, problems, or suggestions...";
        AllowAnonymous = true;
        Categories = new List<FeedbackCategory>();
        MaxResponsesPerParticipant = 5;
    }

    /// <summary>
    /// Parameterized constructor for programmatic creation with validation
    /// </summary>
    [JsonConstructor]
    public GeneralFeedbackConfig(
        int maxLength = 1000,
        int minLength = 10,
        string? placeholder = null,
        bool allowAnonymous = true,
        bool categoriesEnabled = false,
        List<FeedbackCategory>? categories = null,
        bool requireCategory = false,
        bool showCharacterCount = true,
        int maxResponsesPerParticipant = 5)
    {
        if (maxLength < 10)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be at least 10.");
        }

        if (minLength < 0 || minLength > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minLength), "Min length must be between 0 and max length.");
        }

        if (categoriesEnabled && (categories == null || categories.Count == 0))
        {
            throw new ArgumentException("Categories must be provided when categoriesEnabled is true.", nameof(categories));
        }

        if (maxResponsesPerParticipant < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResponsesPerParticipant), "Max responses per participant must be at least 1.");
        }

        MaxLength = maxLength;
        MinLength = minLength;
        Placeholder = placeholder ?? "Share your thoughts, problems, or suggestions...";
        AllowAnonymous = allowAnonymous;
        CategoriesEnabled = categoriesEnabled;
        Categories = categories ?? new List<FeedbackCategory>();
        RequireCategory = requireCategory;
        ShowCharacterCount = showCharacterCount;
        MaxResponsesPerParticipant = maxResponsesPerParticipant;
    }

    public int MaxLength { get; set; } = 1000;
    public int MinLength { get; set; } = 10;
    public string Placeholder { get; set; } = "Share your thoughts, problems, or suggestions...";
    public bool AllowAnonymous { get; set; } = true;
    public bool CategoriesEnabled { get; set; }
    public List<FeedbackCategory> Categories { get; set; } = new();
    public bool RequireCategory { get; set; }
    public bool ShowCharacterCount { get; set; } = true;
    public int MaxResponsesPerParticipant { get; set; } = 5;
}

public sealed class FeedbackCategory
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public FeedbackCategory()
    {
        Id = string.Empty;
        Label = string.Empty;
    }

    /// <summary>
    /// Parameterized constructor for programmatic creation with validation
    /// </summary>
    [JsonConstructor]
    public FeedbackCategory(string id, string label, string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Category ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Category label is required.", nameof(label));
        }

        Id = id.Trim();
        Label = label.Trim();
        Icon = icon?.Trim();
    }

    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
}
