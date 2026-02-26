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

    // ── Reserved namespaces for future activity types ─────────────────────────
    // Naming convention: "<activity-type-slug>:<key-name>"
    // Examples:
    //   "quiz:current_question_index"
    //   "break:started_at"
    //   "fivewhys:active_node_id"
}
