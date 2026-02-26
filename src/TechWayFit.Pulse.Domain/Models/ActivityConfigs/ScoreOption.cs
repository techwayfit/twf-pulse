namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// A single selectable score value for a Quadrant axis.
/// Value is stored as a string so facilitators can use any numbering scheme
/// (integers, Fibonacci, odd numbers, decimals, T-shirt sizes, etc.).
/// </summary>
public sealed class ScoreOption
{
    /// <summary>
    /// The raw value used for averaging (e.g. "1", "3", "5", "8", "13").
    /// Must be parseable as a double for chart averaging.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Short display label shown in the dropdown (e.g. "Simple", "Complex").
    /// If empty, the Value itself is shown.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional longer description shown as a tooltip or sub-text in the dropdown.
    /// Helps participants understand what each score means.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Returns the numeric value for averaging. Returns 0 if not parseable.
    /// </summary>
    public double NumericValue => double.TryParse(Value, System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;

    /// <summary>
    /// Display text shown in the dropdown — label if set, otherwise the raw value.
    /// </summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Label) ? Value : Label;
}
