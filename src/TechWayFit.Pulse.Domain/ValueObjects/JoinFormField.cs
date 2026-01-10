using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Domain.ValueObjects;

public sealed class JoinFormField
{
    public JoinFormField(
        string id,
        string label,
        FieldType type,
        bool required,
        IReadOnlyList<string> options,
        bool useInFilters)
    {
        Id = id.Trim();
        Label = label.Trim();
        Type = type;
        Required = required;
        Options = options;
        UseInFilters = useInFilters;
    }

    public string Id { get; }

    public string Label { get; }

    public FieldType Type { get; }

    public bool Required { get; }

    public IReadOnlyList<string> Options { get; }

    public bool UseInFilters { get; }
}
