namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class UpdateSessionRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public string? Context { get; set; }
    
    public DateTime? SessionStart { get; set; }
    
    public DateTime? SessionEnd { get; set; }
    
    public Guid? GroupId { get; set; }
    
    public bool? StrictCurrentActivityOnly { get; set; }
    
    public bool? AllowAnonymous { get; set; }
    
    public int? TtlMinutes { get; set; }
}

public sealed class UpdateSessionSettingsRequest
{

    public bool StrictCurrentActivityOnly { get; set; }

    public bool AllowAnonymous { get; set; }

    public int TtlMinutes { get; set; }
}

public sealed class UpdateActivityRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public string? Config { get; set; }

    public int? DurationMinutes { get; set; }
}
