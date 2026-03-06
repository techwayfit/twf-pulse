using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

/// <summary>
/// Repository for facilitator subscription management
/// </summary>
public interface IFacilitatorSubscriptionRepository
{
    /// <summary>
    /// Get active subscription for a user (returns null if none)
    /// </summary>
    Task<FacilitatorSubscription?> GetActiveSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    Task<FacilitatorSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
 /// Get all subscriptions for a user (active and historical)
    /// </summary>
    Task<IReadOnlyList<FacilitatorSubscription>> GetUserSubscriptionsAsync(
 Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new subscription
    /// </summary>
    Task AddAsync(FacilitatorSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing subscription
    /// </summary>
    Task UpdateAsync(FacilitatorSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subscriptions that have expired (for background cleanup)
    /// </summary>
    Task<IReadOnlyList<FacilitatorSubscription>> GetExpiredSubscriptionsAsync(
      DateTimeOffset before,
      CancellationToken cancellationToken = default);
}
