namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for General Feedback activity type.
/// Supports long-form text with optional categorization.
/// </summary>
public sealed class GeneralFeedbackConfig
{
    public GeneralFeedbackConfig(
    int maxLength = 1000,
        int minLength = 10,
  string? placeholder = null,
      bool allowAnonymous = true,
        bool categoriesEnabled = false,
    List<FeedbackCategory>? categories = null,
     bool requireCategory = false,
  bool showCharacterCount = true)
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

   MaxLength = maxLength;
    MinLength = minLength;
      Placeholder = placeholder ?? "Share your thoughts, problems, or suggestions...";
        AllowAnonymous = allowAnonymous;
CategoriesEnabled = categoriesEnabled;
        Categories = categories ?? new List<FeedbackCategory>();
     RequireCategory = requireCategory;
        ShowCharacterCount = showCharacterCount;
    }

    public int MaxLength { get; }
    public int MinLength { get; }
  public string Placeholder { get; }
    public bool AllowAnonymous { get; }
    public bool CategoriesEnabled { get; }
    public List<FeedbackCategory> Categories { get; }
 public bool RequireCategory { get; }
    public bool ShowCharacterCount { get; }
}

public sealed class FeedbackCategory
{
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

    public string Id { get; }
    public string Label { get; }
    public string? Icon { get; }
}
