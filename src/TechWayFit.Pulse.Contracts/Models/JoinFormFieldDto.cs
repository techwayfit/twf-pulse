using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.Contracts.Models;

public sealed class JoinFormFieldDto
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public FieldType Type { get; set; }

    public bool Required { get; set; }

    public List<string> Options { get; set; } = new();

    public bool UseInFilters { get; set; }
}
