namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class CreateSessionTemplateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string IconEmoji { get; set; } = "📋";

    public SessionTemplateConfigDto Config { get; set; } = new();
}

public sealed class UpdateSessionTemplateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string IconEmoji { get; set; } = "📋";

    public SessionTemplateConfigDto Config { get; set; } = new();
}

public sealed class SessionTemplateConfigDto
{
    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public string? Context { get; set; }

    public Models.SessionSettingsDto Settings { get; set; } = new();

    public Models.JoinFormSchemaDto JoinFormSchema { get; set; } = new();

    public List<ActivityTemplateRequestDto> Activities { get; set; } = new();
}

public sealed class ActivityTemplateRequestDto
{
    public int Order { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public Models.ActivityConfigDto? Config { get; set; }
}

public sealed class CreateSessionFromTemplateRequest
{
    public Guid TemplateId { get; set; }

    public Guid? GroupId { get; set; }

    /// <summary>
    /// Optional customizations to apply on top of the template
    /// </summary>
    public SessionTemplateCustomizationDto? Customizations { get; set; }
}

public sealed class SessionTemplateCustomizationDto
{
    public string? Title { get; set; }

    public string? Goal { get; set; }

    public string? Context { get; set; }

    public Models.SessionSettingsDto? Settings { get; set; }

    public Models.JoinFormSchemaDto? JoinFormSchema { get; set; }
}
