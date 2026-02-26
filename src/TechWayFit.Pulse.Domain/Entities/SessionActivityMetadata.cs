namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Stores arbitrary transactional/runtime metadata for a session activity.
/// This is a key-value store per (SessionId, ActivityId) pair, designed to hold
/// ephemeral session state that is separate from the immutable activity config.
///
/// Examples:
///   Key = "quadrant:current_item_index"  → Value = "3"
///   Key = "quiz:current_question_index"  → Value = "7"
///   Key = "break:started_at"             → Value = "2026-02-26T10:30:00Z"
///
/// Design rationale:
///   • Not embedded in ActivityConfig so that copying a session/activity (as a template)
///     carries over the configuration but NOT the runtime state.
///   • Generic key-value shape means no schema changes are needed to add new state.
///   • Indexed on (SessionId, ActivityId, Key) for fast point lookups.
/// </summary>
public sealed class SessionActivityMetadata
{
    public SessionActivityMetadata(
        Guid id,
        Guid sessionId,
        Guid activityId,
        string key,
        string value,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        SessionId = sessionId;
        ActivityId = activityId;
        Key = key;
        Value = value;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid SessionId { get; }

    public Guid ActivityId { get; }

    /// <summary>Metadata key, e.g. "quadrant:current_item_index". See <see cref="ActivityMetadataKeys"/>.</summary>
    public string Key { get; }

    /// <summary>Metadata value stored as a string. Numeric values are stored as their string representation.</summary>
    public string Value { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Updates the stored value and refreshes the UpdatedAt timestamp.</summary>
    public void Update(string value)
    {
        Value = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
