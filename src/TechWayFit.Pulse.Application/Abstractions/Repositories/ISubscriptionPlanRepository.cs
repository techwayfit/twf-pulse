using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

/// <summary>
/// Repository for subscription plan data (read-only for main app)
/// </summary>
public interface ISubscriptionPlanRepository
{
    /// <summary>
    /// Get plan by unique code (e.g., "free", "plan-a")
    /// </summary>
    Task<SubscriptionPlan?> GetByCodeAsync(string planCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get plan by ID
    /// </summary>
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active plans (for pricing page)
    /// </summary>
    Task<IReadOnlyList<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a subscription plan (read from database)
/// NOTE: This is NOT a full domain entity - it's a read model for the main app
/// </summary>
public sealed record SubscriptionPlan(
    Guid Id,
    string PlanCode,
    string DisplayName,
  string? Description,
    decimal PriceMonthly,
    decimal? PriceYearly,
    int MaxSessionsPerMonth,
    string FeaturesJson,
    bool IsActive,
    int SortOrder);
