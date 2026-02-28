namespace TechWayFit.Pulse.Domain.Models;

/// <summary>
/// Well-known keys for <see cref="TechWayFit.Pulse.Domain.Entities.SessionActivityMetadata"/>.
/// Always use these constants instead of raw strings to ensure consistency.
/// </summary>
public static class ActivityMetadataKeys
{
    // ── Quadrant / Item Scoring ───────────────────────────────────────────────

    /// <summary>
    /// Zero-based index of the item the facilitator currently has open.
    /// Value: integer as string, e.g. "0", "1", "12".
    /// </summary>
    public const string QuadrantCurrentItemIndex = "quadrant:current_item_index";

    // ── Break ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// UTC timestamp (ISO-8601 round-trip format) of when the break countdown was first started.
    /// Value: DateTimeOffset as string, e.g. "2024-06-01T10:30:00.0000000+00:00".
    /// Used to resume the countdown from the correct position after a page refresh.
    /// </summary>
    public const string BreakStartedAt = "break:started_at";

    // ── Reserved namespaces for future activity types ─────────────────────────
    // Naming convention: "<activity-type-slug>:<key-name>"
    // Examples:
    //   "quiz:current_question_index"
    //   "fivewhys:active_node_id"
}
