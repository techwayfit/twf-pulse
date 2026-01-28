using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Poll activity type.
/// Supports single or multiple choice questions with optional custom answers.
/// </summary>
public sealed class PollConfig
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public PollConfig()
    {
        Options = new List<PollOption>();
        CustomOptionPlaceholder = "Other (please specify)";
        MinSelections = 1;
        MaxResponsesPerParticipant = 1;
    }

    /// <summary>
    /// Parameterized constructor for programmatic creation with validation
    /// </summary>
    [JsonConstructor]
    public PollConfig(
        List<PollOption> options,
        bool allowMultiple = false,
        int minSelections = 1,
        int? maxSelections = null,
        bool allowCustomOption = false,
        string? customOptionPlaceholder = null,
        bool randomizeOrder = false,
        bool showResultsAfterSubmit = false,
        int maxResponsesPerParticipant = 1)
    {
        if (options == null || options.Count == 0)
        {
            throw new ArgumentException("Poll must have at least one option.", nameof(options));
        }

        if (minSelections < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minSelections), "Minimum selections cannot be negative.");
        }

        if (maxSelections.HasValue && maxSelections.Value < minSelections)
        {
            throw new ArgumentException("Maximum selections cannot be less than minimum selections.");
        }

        if (maxResponsesPerParticipant < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResponsesPerParticipant), "Max responses per participant must be at least 1.");
        }

        Options = options;
        AllowMultiple = allowMultiple;
        MinSelections = minSelections;
        MaxSelections = maxSelections ?? (allowMultiple ? options.Count : 1);
        AllowCustomOption = allowCustomOption;
        CustomOptionPlaceholder = customOptionPlaceholder ?? "Other (please specify)";
        RandomizeOrder = randomizeOrder;
        ShowResultsAfterSubmit = showResultsAfterSubmit;
        MaxResponsesPerParticipant = maxResponsesPerParticipant;
    }

    public List<PollOption> Options { get; set; } = new();
    public bool AllowMultiple { get; set; }
    public int MinSelections { get; set; } = 1;
    public int? MaxSelections { get; set; }
    public bool AllowCustomOption { get; set; }
    public string CustomOptionPlaceholder { get; set; } = "Other (please specify)";
    public bool RandomizeOrder { get; set; }
    public bool ShowResultsAfterSubmit { get; set; }
    public int MaxResponsesPerParticipant { get; set; } = 1;
}

public sealed class PollOption
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public PollOption()
    {
        Id = string.Empty;
        Label = string.Empty;
    }

    /// <summary>
    /// Parameterized constructor for programmatic creation with validation
    /// </summary>
    [JsonConstructor]
    public PollOption(string id, string label, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Option ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Option label is required.", nameof(label));
        }

        Id = id.Trim();
        Label = label.Trim();
        Description = description?.Trim();
    }

    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
}
