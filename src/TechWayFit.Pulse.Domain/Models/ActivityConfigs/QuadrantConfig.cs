namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for the rebuilt Quadrant / Item Scoring activity type.
/// The facilitator defines a list of items (questions/topics) and custom score
/// options for each axis. During the session the facilitator steps through items
/// one at a time; participants score each item using dropdowns. The dashboard
/// aggregates responses into a bubble chart.
/// </summary>
public sealed class QuadrantConfig
{
    public QuadrantConfig()
    {
        XAxisLabel = "Complexity";
        YAxisLabel = "Effort";
        XScoreOptions = DefaultNumericOptions(1, 10);
        YScoreOptions = new List<ScoreOption>(); // empty = share X options
        Items = new List<string>();
        BubbleSizeMode = QuadrantBubbleSizeMode.Proportional;
    }

    // ── Axis labels ──────────────────────────────────────────────────────────

    /// <summary>Label for the horizontal axis (e.g. "Complexity", "Impact").</summary>
    public string XAxisLabel { get; set; }

    /// <summary>Label for the vertical axis (e.g. "Effort", "Risk").</summary>
    public string YAxisLabel { get; set; }

    // ── Score options ─────────────────────────────────────────────────────────

    /// <summary>
    /// Selectable score values for the X axis.
    /// Each option has a Value (used for averaging), an optional Label (shown in dropdown),
    /// and an optional Description (shown as tooltip/help text).
    /// </summary>
    public List<ScoreOption> XScoreOptions { get; set; }

    /// <summary>
    /// Selectable score values for the Y axis.
    /// When empty the X options are reused for Y as well.
    /// </summary>
    public List<ScoreOption> YScoreOptions { get; set; }

    /// <summary>True when the Y axis should reuse the X options.</summary>
    public bool YSharesXOptions => YScoreOptions == null || YScoreOptions.Count == 0;

    /// <summary>Resolved Y options — falls back to X options when Y is empty.</summary>
    public List<ScoreOption> ResolvedYScoreOptions =>
        YSharesXOptions ? XScoreOptions : YScoreOptions;

    // ── Items ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ordered list of items / questions to score.
    /// The facilitator steps through these one at a time during the session.
    /// Maximum 200 items.
    /// </summary>
    public List<string> Items { get; set; }

    // ── Chart display ─────────────────────────────────────────────────────────

    /// <summary>
    /// Whether bubble radius is proportional to the number of responses (Proportional)
    /// or fixed/uniform for all bubbles (Uniform).
    /// Can also be toggled live in the dashboard chart options.
    /// </summary>
    public QuadrantBubbleSizeMode BubbleSizeMode { get; set; }

    /// <summary>Optional note field for participants per item (true = show note input).</summary>
    public bool AllowNotes { get; set; }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Generates a list of simple integer score options from <paramref name="min"/> to <paramref name="max"/>.</summary>
    public static List<ScoreOption> DefaultNumericOptions(int min, int max)
    {
        var list = new List<ScoreOption>();
        for (var i = min; i <= max; i++)
            list.Add(new ScoreOption { Value = i.ToString() });
        return list;
    }

    /// <summary>Standard Fibonacci preset: 1, 2, 3, 5, 8, 13.</summary>
    public static List<ScoreOption> FibonacciOptions() =>
        new[] { 1, 2, 3, 5, 8, 13 }
            .Select(v => new ScoreOption { Value = v.ToString() })
            .ToList();

    /// <summary>Odd numbers preset: 1, 3, 5, 7, 9.</summary>
    public static List<ScoreOption> OddOptions() =>
        new[] { 1, 3, 5, 7, 9 }
            .Select(v => new ScoreOption { Value = v.ToString() })
            .ToList();
}

/// <summary>Controls how bubble radius is rendered in the bubble chart.</summary>
public enum QuadrantBubbleSizeMode
{
    /// <summary>Bubble radius scales with the number of responses for that item.</summary>
    Proportional = 0,

    /// <summary>All bubbles use the same fixed radius.</summary>
    Uniform = 1
}
