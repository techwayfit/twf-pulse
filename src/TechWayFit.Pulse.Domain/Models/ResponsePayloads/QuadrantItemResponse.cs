namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Response payload for the rebuilt Quadrant / Item Scoring activity.
/// One record is submitted per item per participant.
/// The xValue and yValue are the raw string values from the ScoreOption the
/// participant selected (e.g. "5", "8", "13").
/// </summary>
public sealed class QuadrantItemResponse
{
    /// <summary>
    /// Zero-based index of the item being scored (matches QuadrantConfig.Items[ItemIndex]).
    /// </summary>
    public int ItemIndex { get; set; }

    /// <summary>
    /// Raw score value chosen for the X axis (e.g. "5").
    /// Must match one of the QuadrantConfig.XScoreOptions values.
    /// </summary>
    public string XValue { get; set; } = string.Empty;

    /// <summary>
    /// Raw score value chosen for the Y axis (e.g. "8").
    /// Must match one of the resolved Y score options.
    /// </summary>
    public string YValue { get; set; } = string.Empty;

    /// <summary>
    /// Optional free-text note the participant can add when AllowNotes is enabled.
    /// </summary>
    public string? Note { get; set; }
}
