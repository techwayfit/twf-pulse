using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using RepoSubscriptionPlan = TechWayFit.Pulse.Application.Abstractions.Repositories.SubscriptionPlan;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Service for managing subscription plans, quotas, and feature access.
/// Handles session creation limits, premium feature gates, and activity type access.
/// </summary>
public interface IPlanService
{
    /// <summary>
    /// Get all active plans (for pricing page)
    /// </summary>
Task<IReadOnlyList<RepoSubscriptionPlan>> GetAvailablePlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
  /// Get plan by unique code (e.g., "free", "plan-a", "plan-b")
    /// </summary>
    Task<RepoSubscriptionPlan?> GetPlanByCodeAsync(string planCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed plan status for a facilitator (quota usage, features, etc.)
    /// </summary>
    Task<PlanStatus> GetPlanStatusAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active subscription for a facilitator (returns null if none)
    /// </summary>
    Task<FacilitatorSubscription?> GetActiveSubscriptionAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if facilitator can create a new session (has quota remaining)
    /// </summary>
    Task<bool> CanCreateSessionAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consume one session from monthly quota (called after successful session creation)
    /// </summary>
    Task ConsumeSessionAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if facilitator can use a premium feature (aiAssist, aiSummary, etc.)
 /// </summary>
    Task<bool> CanUseFeatureAsync(Guid facilitatorUserId, string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if facilitator can use a specific activity type based on their plan
    /// </summary>
    Task<bool> CanUseActivityTypeAsync(Guid facilitatorUserId, ActivityType activityType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Detailed plan status for a facilitator
/// </summary>
public sealed record PlanStatus(
    string PlanCode,
    string PlanDisplayName,
    int SessionsUsed,
    int SessionsAllowed,
    DateTimeOffset SessionsResetAt,
  DateTimeOffset? ExpiresAt,
    string Status,
    PlanFeaturesDto Features);

/// <summary>
/// Feature access flags for a plan
/// </summary>
public sealed record PlanFeaturesDto(
    bool AiAssist,
    bool AiSummary,
    Dictionary<ActivityType, bool> ActivityAccess);
