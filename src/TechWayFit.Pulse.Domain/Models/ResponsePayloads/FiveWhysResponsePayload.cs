namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Represents one exchange in the 5 Whys chain — a question and the participant's answer.
/// </summary>
public sealed class FiveWhysExchange
{
    /// <summary>Depth level (1 = initial question, 2 = first follow-up, etc.).</summary>
    public int Level { get; set; }

    /// <summary>The question asked at this level (set by AI for levels > 1, or from config for level 1).</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>The participant's answer at this level.</summary>
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// Response payload for the 5 Whys AI activity.
/// Stores the full chain of questions and answers, plus the final root cause if identified.
/// </summary>
public sealed class FiveWhysResponsePayload
{
    /// <summary>
    /// The sequence of Q&amp;A exchanges in the root cause journey.
    /// Ordered from level 1 (initial problem) to the deepest level reached.
    /// </summary>
    public List<FiveWhysExchange> Chain { get; set; } = new();

    /// <summary>
    /// When true, AI has determined the root cause and the journey is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// The root cause statement identified by AI. Populated only when IsComplete is true.
    /// </summary>
    public string? RootCause { get; set; }

    /// <summary>
    /// A brief AI insight explaining why this is the root cause and what it means.
    /// Populated only when IsComplete is true.
    /// </summary>
    public string? Insight { get; set; }
}
