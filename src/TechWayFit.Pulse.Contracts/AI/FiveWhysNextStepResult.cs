namespace TechWayFit.Pulse.Contracts.AI;

/// <summary>
/// Represents one completed exchange in the 5 Whys chain (sent to the AI for context).
/// </summary>
public sealed class FiveWhysChainEntry
{
    public int Level { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// Result returned by the AI for a single 5 Whys step.
/// </summary>
public sealed class FiveWhysNextStepResult
{
    /// <summary>
    /// The next follow-up question the AI wants to ask.
    /// Null when IsComplete is true.
    /// </summary>
    public string? NextQuestion { get; set; }

    /// <summary>
    /// True when the AI has identified the root cause and no more questions are needed.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// The root cause statement. Populated only when IsComplete is true.
    /// </summary>
    public string? RootCause { get; set; }

    /// <summary>
    /// A brief insight explaining why this is the root cause and recommended actions.
    /// Populated only when IsComplete is true.
    /// </summary>
    public string? Insight { get; set; }
}
