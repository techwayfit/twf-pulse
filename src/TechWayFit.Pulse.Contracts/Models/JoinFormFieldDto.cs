using TechWayFit.Pulse.Contracts.Enums;
using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Contracts.Models;

public sealed class JoinFormFieldDto
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public FieldType Type { get; set; }

    public bool Required { get; set; }

    // Store options as a string (comma-separated values)
    public string Options { get; set; } = string.Empty;

    public bool UseInFilters { get; set; }

    // Helper method to get options as a list
    [JsonIgnore]
    public List<string> OptionsList
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Options))
                return new List<string>();

            return Options
                .Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
    }

    // Helper method to set options from a list
    public void SetOptions(IEnumerable<string> optionsList)
    {
        Options = string.Join(",", optionsList?.Where(s => !string.IsNullOrWhiteSpace(s)) ?? Array.Empty<string>());
    }
}
