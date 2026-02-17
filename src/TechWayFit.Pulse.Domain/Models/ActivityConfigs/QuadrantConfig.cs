namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Quadrant activity type.
/// Supports 2D mapping across two dimensions with customizable axis labels.
/// </summary>
public sealed class QuadrantConfig
{
    public QuadrantConfig()
    {
        XAxisLabel = "X Axis";
        YAxisLabel = "Y Axis";
        TopLeftLabel = "Top Left";
        TopRightLabel = "Top Right";
        BottomLeftLabel = "Bottom Left";
        BottomRightLabel = "Bottom Right";
        Scale = 10;
        AllowLabels = true;
        MaxLabelLength = 100;
        MaxPointsPerParticipant = 1;
    }

    /// <summary>
    /// Label for the horizontal axis (e.g., "Effort Required", "Complexity")
    /// </summary>
    public string XAxisLabel { get; set; }

    /// <summary>
    /// Label for the vertical axis (e.g., "Impact", "Value", "Priority")  
    /// </summary>
    public string YAxisLabel { get; set; }

    /// <summary>
    /// Description for top-left quadrant (low X, high Y)
    /// </summary>
    public string TopLeftLabel { get; set; }

    /// <summary>
    /// Description for top-right quadrant (high X, high Y)
    /// </summary>
    public string TopRightLabel { get; set; }

    /// <summary>
    /// Description for bottom-left quadrant (low X, low Y)
    /// </summary>
    public string BottomLeftLabel { get; set; }

    /// <summary>
    /// Description for bottom-right quadrant (high X, high Y)
    /// </summary>
    public string BottomRightLabel { get; set; }

    /// <summary>
    /// The scale for scoring each axis (e.g., 1-10 means participants score from 1 to 10)
    /// Default is 10 (1-10 scale)
    /// </summary>
    public int Scale { get; set; }

    /// <summary>
    /// Whether participants can add text labels to their points
    /// </summary>
    public bool AllowLabels { get; set; }

    /// <summary>
    /// Maximum length for point labels
    /// </summary>
    public int MaxLabelLength { get; set; }

    /// <summary>
    /// Maximum number of points a participant can place
    /// </summary>
    public int MaxPointsPerParticipant { get; set; }
}
