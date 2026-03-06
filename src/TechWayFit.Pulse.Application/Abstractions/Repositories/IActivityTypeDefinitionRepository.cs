using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

/// <summary>
/// Repository for activity type definition data (read-only for main app)
/// Definitions are managed via BackOffice
/// </summary>
public interface IActivityTypeDefinitionRepository
{
    /// <summary>
    /// Get activity type definition by ActivityType enum value
    /// </summary>
    Task<ActivityTypeDefinition?> GetByActivityTypeAsync(ActivityType activityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active activity type definitions (for activity picker)
    /// Ordered by SortOrder ascending
    /// </summary>
    Task<IReadOnlyList<ActivityTypeDefinition>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get activity type definition by ID
    /// </summary>
    Task<ActivityTypeDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
