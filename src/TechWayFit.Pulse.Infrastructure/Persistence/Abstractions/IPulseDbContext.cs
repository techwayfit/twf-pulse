using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;

/// <summary>
/// Common interface for all TechWayFit Pulse database contexts.
/// Defines all entity sets required by repositories.
/// </summary>
public interface IPulseDbContext : IDisposable
{
    /// <summary>
    /// Workshop sessions
/// </summary>
    DbSet<SessionRecord> Sessions { get; }

 /// <summary>
    /// Activities within sessions
/// </summary>
    DbSet<ActivityRecord> Activities { get; }

/// <summary>
  /// Workshop participants
    /// </summary>
    DbSet<ParticipantRecord> Participants { get; }

    /// <summary>
/// Participant responses to activities
    /// </summary>
    DbSet<ResponseRecord> Responses { get; }

    /// <summary>
    /// Contribution counters per participant
    /// </summary>
    DbSet<ContributionCounterRecord> ContributionCounters { get; }

    /// <summary>
    /// Facilitator user accounts
    /// </summary>
    DbSet<FacilitatorUserRecord> FacilitatorUsers { get; }

  /// <summary>
    /// Key-value storage for facilitator preferences
    /// </summary>
 DbSet<FacilitatorUserDataRecord> FacilitatorUserData { get; }

    /// <summary>
    /// One-time passwords for authentication
    /// </summary>
    DbSet<LoginOtpRecord> LoginOtps { get; }

/// <summary>
/// Hierarchical session organization
    /// </summary>
    DbSet<SessionGroupRecord> SessionGroups { get; }

    /// <summary>
    /// Reusable session templates
    /// </summary>
    DbSet<SessionTemplateRecord> SessionTemplates { get; }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity entry for change tracking.
    /// </summary>
    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry(object entity);

    /// <summary>
    /// Creates a DbSet for the given entity type.
    /// </summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
}
