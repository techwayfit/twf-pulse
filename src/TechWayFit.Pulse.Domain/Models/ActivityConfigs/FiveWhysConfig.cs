namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for the 5 Whys AI-driven root cause analysis activity.
/// The facilitator sets the initial problem statement and context.
/// AI progressively digs deeper by asking follow-up "why" questions based on participant answers.
/// </summary>
public sealed class FiveWhysConfig
{
    /// <summary>
    /// The initial problem/question the participant must answer first.
    /// e.g. "Why did our last sprint fail to meet its goals?"
    /// </summary>
    public string RootQuestion { get; set; } = "Why is this problem occurring?";

    /// <summary>
    /// Optional background context provided by the facilitator.
    /// Sent to the AI to help it generate more targeted follow-up questions.
    /// e.g. "We had 3 failed deployments this sprint, causing a 2-hour outage."
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Maximum number of follow-up "why" levels the AI will drill down (default 5).
    /// AI may stop earlier if it identifies the root cause before reaching max depth.
    /// </summary>
    public int MaxDepth { get; set; } = 5;
}
