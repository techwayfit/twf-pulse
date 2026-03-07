using System.ComponentModel.DataAnnotations;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Core.Models.Templates;

public sealed class SaveTemplateRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TemplateCategory Category { get; set; }

    [Required, MaxLength(10)]
    public string IconEmoji { get; set; } = string.Empty;

    /// <summary>Full JSON config string (SessionTemplateConfig).</summary>
    [Required]
    public string ConfigJson { get; set; } = string.Empty;
}
