using TechWayFit.Pulse.Contracts.Models;

namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class CreateSessionRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    /// <summary>
    /// Legacy context field (kept for backward compatibility)
    /// </summary>
    public string? Context { get; set; }

    public SessionSettingsDto Settings { get; set; } = new();

    public JoinFormSchemaDto JoinFormSchema { get; set; } = new();

    public Guid? GroupId { get; set; }
    
    /// <summary>
    /// Planned start date/time for the workshop session (optional)
    /// Used for planning purposes only, does not auto-start the session
    /// </summary>
    public DateTime? SessionStart { get; set; }
    
    /// <summary>
    /// Planned end date/time for the workshop session (optional)
    /// Used for planning purposes only, does not auto-end the session
    /// </summary>
    public DateTime? SessionEnd { get; set; }
    
    /// <summary>
    /// Enhanced context for AI session generation (optional)
    /// When provided, AI will use this rich context to generate better activities
    /// </summary>
    public SessionGenerationContextDto? GenerationContext { get; set; }
    
    /// <summary>
    /// AI generation options (optional)
    /// </summary>
    public SessionGenerationOptionsDto? GenerationOptions { get; set; }
}
