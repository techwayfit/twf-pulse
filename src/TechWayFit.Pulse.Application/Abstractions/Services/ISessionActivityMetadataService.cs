namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Application service for reading and writing session activity metadata.
/// Metadata holds transient runtime state (e.g. current item index) that is
/// separate from the immutable activity <c>Config</c> JSON.
///
/// Separation from config ensures that copying a session/template copies the
/// activity shape only — never its live execution state.
/// </summary>
public interface ISessionActivityMetadataService
{
    /// <summary>
    /// Returns the stored value for the given key, or <c>null</c> if not yet set.
    /// </summary>
    Task<string?> GetValueAsync(Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates (upserts) the value for the given key.
    /// </summary>
    Task SetValueAsync(Guid sessionId, Guid activityId, string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all metadata for the given (session, activity) as a read-only dictionary.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a single metadata entry. No-op if the entry does not exist.
    /// </summary>
    Task DeleteAsync(Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes ALL metadata entries for a (session, activity) pair.
    /// Typically called when an activity is reset or deleted.
    /// </summary>
    Task DeleteAllForActivityAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default);
}
