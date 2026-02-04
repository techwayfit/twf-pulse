using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Domain.Models;

/// <summary>
/// Simplified template configuration for activity-only templates (no session metadata)
/// This reduces template size and AI token usage
/// </summary>
public sealed class ActivitySetTemplateConfig
{
    /// <summary>
    /// Name of the activity set (e.g., "Retrospective Activities", "Ops Improvement Flow")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of when to use this activity set
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// List of activities in order
    /// </summary>
    public List<ActivityTemplateConfig> Activities { get; set; } = new();
}
