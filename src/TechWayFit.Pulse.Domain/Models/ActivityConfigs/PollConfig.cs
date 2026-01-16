namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Poll activity type.
/// Supports single or multiple choice questions with optional custom answers.
/// </summary>
public sealed class PollConfig
{
    public PollConfig(
        List<PollOption> options,
        bool allowMultiple = false,
        int minSelections = 1,
int? maxSelections = null,
        bool allowCustomOption = false,
      string? customOptionPlaceholder = null,
        bool randomizeOrder = false,
        bool showResultsAfterSubmit = false)
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

        Options = options;
  AllowMultiple = allowMultiple;
        MinSelections = minSelections;
    MaxSelections = maxSelections ?? (allowMultiple ? options.Count : 1);
        AllowCustomOption = allowCustomOption;
 CustomOptionPlaceholder = customOptionPlaceholder ?? "Other (please specify)";
        RandomizeOrder = randomizeOrder;
  ShowResultsAfterSubmit = showResultsAfterSubmit;
  }

  public List<PollOption> Options { get; }
    public bool AllowMultiple { get; }
    public int MinSelections { get; }
    public int MaxSelections { get; }
    public bool AllowCustomOption { get; }
    public string CustomOptionPlaceholder { get; }
    public bool RandomizeOrder { get; }
    public bool ShowResultsAfterSubmit { get; }
}

public sealed class PollOption
{
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

    public string Id { get; }
    public string Label { get; }
    public string? Description { get; }
}
