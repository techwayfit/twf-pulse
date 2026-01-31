namespace TechWayFit.Pulse.Contracts.Models;

public class SessionTemplateDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string IconEmoji { get; set; } = string.Empty;

    public bool IsSystemTemplate { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SessionTemplateDetailDto : SessionTemplateDto
{
    public SessionTemplateConfigDto Config { get; set; } = new();
}

public sealed class SessionTemplateConfigDto
{
    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public string? Context { get; set; }

    public SessionSettingsDto Settings { get; set; } = new();

    public JoinFormSchemaDto JoinFormSchema { get; set; } = new();

    public List<ActivityTemplateDto> Activities { get; set; } = new();
}

public sealed class ActivityTemplateDto
{
    public int Order { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public ActivityConfigDto? Config { get; set; }
}

public sealed class ActivityConfigDto
{
    // Poll/Quiz
    public List<PollOptionDto>? Options { get; set; }

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
    public List<FeedbackCategoryDto>? Categories { get; set; }
}

public sealed class PollOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class FeedbackCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
}
