using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

/// <summary>
/// Repository for session activity metadata — transient runtime state scoped to a single
/// live session, separate from the immutable activity configuration.
/// </summary>
public interface ISessionActivityMetadataRepository
{
    /// <summary>Returns the metadata record for a specific (session, activity, key) triple, or null if absent.</summary>
    Task<SessionActivityMetadata?> GetAsync(Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default);

    /// <summary>Returns all metadata records for a given (session, activity) pair.</summary>
    Task<IReadOnlyList<SessionActivityMetadata>> GetAllAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the value for (session, activity, key).
    /// This is an upsert — callers do not need to check for existence first.
    /// </summary>
    Task UpsertAsync(Guid sessionId, Guid activityId, string key, string value, CancellationToken cancellationToken = default);

    /// <summary>Deletes a single metadata entry. No-op if the entry does not exist.</summary>
    Task DeleteAsync(Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes all metadata entries for a (session, activity) pair, e.g. when resetting an activity.</summary>
    Task DeleteAllForActivityAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>Deletes all metadata entries for an entire session, e.g. when archiving or deleting it.</summary>
    Task DeleteAllForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
