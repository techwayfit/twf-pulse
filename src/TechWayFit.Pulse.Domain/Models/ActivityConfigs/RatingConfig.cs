using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Rating activity type.
/// Supports numeric scales (1-5, 1-10, etc.) with optional comments.
/// </summary>
public sealed class RatingConfig
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public RatingConfig()
    {
        Scale = 5;
        MinLabel = "1 - Low";
        MaxLabel = "5 - High";
        CommentPlaceholder = "Tell us more (optional)";
        DisplayType = RatingDisplayType.Buttons;
        MaxResponsesPerParticipant = 1;
    }

    [JsonConstructor]
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

    public int Scale { get; set; } = 5;
    public string MinLabel { get; set; } = "1 - Low";
    public string MaxLabel { get; set; } = "5 - High";
    public string? MidpointLabel { get; set; }
    public bool AllowComments { get; set; } = true;
    public bool CommentRequired { get; set; }
    public string CommentPlaceholder { get; set; } = "Tell us more (optional)";
    public RatingDisplayType DisplayType { get; set; } = RatingDisplayType.Buttons;
    public bool ShowAverageAfterSubmit { get; set; }
    public int MaxResponsesPerParticipant { get; set; } = 1;
}

public enum RatingDisplayType
{
    Buttons = 0,
    Slider = 1,
    Stars = 2
}
