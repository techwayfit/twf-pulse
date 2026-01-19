namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Rating activity type.
/// Supports numeric scales (1-5, 1-10, etc.) with optional comments.
/// </summary>
public sealed class RatingConfig
{
    public RatingConfig(
      int scale = 5,
   string? minLabel = null,
        string? maxLabel = null,
        string? midpointLabel = null,
   bool allowComments = true,
        bool commentRequired = false,
        string? commentPlaceholder = null,
     RatingDisplayType displayType = RatingDisplayType.Buttons,
   bool showAverageAfterSubmit = false,
        int maxResponsesPerParticipant = 1)
    {
        if (scale < 2 || scale > 10)
     {
  throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be between 2 and 10.");
        }

        if (maxResponsesPerParticipant < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResponsesPerParticipant), "Max responses per participant must be at least 1.");
        }

     Scale = scale;
        MinLabel = minLabel ?? $"1 - Low";
        MaxLabel = maxLabel ?? $"{scale} - High";
        MidpointLabel = midpointLabel;
      AllowComments = allowComments;
        CommentRequired = commentRequired;
        CommentPlaceholder = commentPlaceholder ?? "Tell us more (optional)";
      DisplayType = displayType;
        ShowAverageAfterSubmit = showAverageAfterSubmit;
        MaxResponsesPerParticipant = maxResponsesPerParticipant;
    }

    public int Scale { get; }
    public string MinLabel { get; }
    public string MaxLabel { get; }
    public string? MidpointLabel { get; }
    public bool AllowComments { get; }
    public bool CommentRequired { get; }
    public string CommentPlaceholder { get; }
    public RatingDisplayType DisplayType { get; }
  public bool ShowAverageAfterSubmit { get; }
    public int MaxResponsesPerParticipant { get; } = 1;
}

public enum RatingDisplayType
{
    Buttons = 0,
    Slider = 1,
    Stars = 2
}
