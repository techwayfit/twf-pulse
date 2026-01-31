using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;

namespace TechWayFit.Pulse.Domain.Models;

/// <summary>
/// Configuration model for session templates (serialized to JSON)
/// </summary>
public sealed class SessionTemplateConfig
{
    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public string? Context { get; set; }

    public SessionSettingsConfig Settings { get; set; } = new();

    public JoinFormSchemaConfig JoinFormSchema { get; set; } = new();

    public List<ActivityTemplateConfig> Activities { get; set; } = new();
}

public sealed class SessionSettingsConfig
{
    public int? DurationMinutes { get; set; }

    public int? MaxParticipants { get; set; }

    public bool AllowAnonymous { get; set; } = true;

    public bool AllowLateJoin { get; set; } = true;

    public bool ShowResultsDuringActivity { get; set; }
}

public sealed class JoinFormSchemaConfig
{
    public List<JoinFormFieldConfig> Fields { get; set; } = new();
}

public sealed class JoinFormFieldConfig
{
    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Type { get; set; } = "text";

    public bool Required { get; set; }

    public List<string> Options { get; set; } = new();
}

public sealed class ActivityTemplateConfig
{
    public int Order { get; set; }

    public ActivityType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public int? DurationMinutes { get; set; }

    public ActivityConfigData? Config { get; set; }
}

/// <summary>
/// Flexible activity configuration data
/// </summary>
public sealed class ActivityConfigData
{
    // Poll/Quiz
    public List<PollOption>? Options { get; set; }

    public bool? MultipleChoice { get; set; }

    public int? CorrectOptionIndex { get; set; }

    // Rating
    public int? MaxRating { get; set; }

    public string? RatingLabel { get; set; }

    // Quadrant
    public string? XAxisLabel { get; set; }

    public string? YAxisLabel { get; set; }

    public string? TopLeftLabel { get; set; }

    public string? TopRightLabel { get; set; }

    public string? BottomLeftLabel { get; set; }

    public string? BottomRightLabel { get; set; }

    // Five Whys
    public int? MaxDepth { get; set; }

    // Word Cloud
    public int? MaxWords { get; set; }

    public int? MinWordLength { get; set; }

    // General Feedback
    public List<FeedbackCategory>? Categories { get; set; }
}
