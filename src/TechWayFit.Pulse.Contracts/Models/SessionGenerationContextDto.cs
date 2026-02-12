namespace TechWayFit.Pulse.Contracts.Models;

/// <summary>
/// Enhanced context for AI session generation
/// </summary>
public class SessionGenerationContextDto
{
    public string? WorkshopType { get; set; }
    
    public int? DurationMinutes { get; set; }
    
    public int? ParticipantCount { get; set; }
    
    public ParticipantTypesDto? ParticipantTypes { get; set; }
    
    public List<string> Goals { get; set; } = new();
    
    public List<string> Constraints { get; set; } = new();
    
    public string? Tone { get; set; }
    
    public List<string>? IncludeActivityTypes { get; set; }
    
    public List<string>? ExcludeActivityTypes { get; set; }
    
    /// <summary>
    /// Target number of activities to generate (overrides duration-based calculation)
    /// </summary>
    public int? TargetActivityCount { get; set; }
    
    /// <summary>
    /// List of existing activities in format "Title | Type" to avoid duplicates
    /// </summary>
    public string? ExistingActivities { get; set; }
    
    public ContextDocumentsDto? ContextDocuments { get; set; }
}

/// <summary>
/// Participant type information for targeted activity generation
/// </summary>
public class ParticipantTypesDto
{
    /// <summary>
    /// Primary participant type: Technical, Managers, Business, Leaders, Mixed
    /// </summary>
    public string? Primary { get; set; }
    
    /// <summary>
    /// Breakdown of participant counts by type
    /// </summary>
    public Dictionary<string, int>? Breakdown { get; set; }
    
    /// <summary>
    /// Experience level distribution (junior, midLevel, senior, expert)
    /// </summary>
    public Dictionary<string, int>? ExperienceLevels { get; set; }
    
    /// <summary>
    /// Specific roles (e.g., "Backend Engineer", "QA", "DevOps")
    /// </summary>
    public List<string> CustomRoles { get; set; } = new();
}

/// <summary>
/// Context documents that provide specific information for AI to reference
/// </summary>
public class ContextDocumentsDto
{
    public SprintBacklogDto? SprintBacklog { get; set; }
    
    public IncidentReportDto? IncidentReport { get; set; }
    
    public ProductDocumentationDto? ProductDocumentation { get; set; }
    
    /// <summary>
    /// Multiple custom documents (strategy docs, training materials, etc.)
    /// </summary>
    public List<CustomDocumentDto> CustomDocuments { get; set; } = new();
}

/// <summary>
/// Sprint backlog information for retrospectives or planning sessions
/// </summary>
public class SprintBacklogDto
{
    public bool Provided { get; set; }
    
    /// <summary>
    /// Brief summary of sprint (max 500 chars, will be sanitized)
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Top 5-10 backlog items or key stories
    /// </summary>
    public List<string> KeyItems { get; set; } = new();
}

/// <summary>
/// Incident report information for post-mortems
/// </summary>
public class IncidentReportDto
{
    public bool Provided { get; set; }
    
    /// <summary>
    /// Brief summary of incident (max 500 chars, will be sanitized)
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Severity: P0, P1, P2, P3, P4
    /// </summary>
    public string? Severity { get; set; }
    
    /// <summary>
    /// Systems or services impacted
    /// </summary>
    public List<string> ImpactedSystems { get; set; } = new();
    
    public int? DurationMinutes { get; set; }
    
    public string? CustomersImpacted { get; set; }
}

/// <summary>
/// Product documentation for feature feedback or discovery sessions
/// </summary>
public class ProductDocumentationDto
{
    public bool Provided { get; set; }
    
    /// <summary>
    /// Brief summary of product/feature (max 500 chars, will be sanitized)
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Key features to discuss
    /// </summary>
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// Custom document (strategy docs, training materials, etc.)
/// </summary>
public class CustomDocumentDto
{
    public bool Provided { get; set; }
    
    /// <summary>
    /// Type of document (e.g., "Strategy Document", "Customer Feedback")
    /// </summary>
    public string? Type { get; set; }
    
    /// <summary>
    /// Brief summary (max 500 chars, will be sanitized)
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Key points from the document
    /// </summary>
    public List<string> KeyPoints { get; set; } = new();
}

/// <summary>
/// AI generation options
/// </summary>
public class SessionGenerationOptionsDto
{
    /// <summary>
    /// AI provider: AzureOpenAI, OpenAI
    /// </summary>
    public string? AiProvider { get; set; }
    
    /// <summary>
    /// Model to use (e.g., gpt-4, gpt-4o-mini, gpt-3.5-turbo)
    /// </summary>
    public string? Model { get; set; }
    
    /// <summary>
    /// Temperature (0.0-1.0, default 0.7)
    /// </summary>
    public double? Temperature { get; set; }
    
    /// <summary>
    /// Whether to return multiple options
    /// </summary>
    public bool ReturnMultipleOptions { get; set; }
    
    /// <summary>
    /// Number of options to return (1-3)
    /// </summary>
    public int OptionsCount { get; set; } = 1;
}
