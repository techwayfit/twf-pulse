namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Defines a subscription plan with quota limits and feature access.
/// System-defined, operator-managed via BackOffice. Not created by facilitators.
/// Seed data includes: Free (2 sessions), Plan A (5 sessions), Plan B (15 sessions).
/// </summary>
public sealed class SubscriptionPlan
{
    public SubscriptionPlan(
        Guid id,
        string planCode,
      string displayName,
        string? description,
 decimal priceMonthly,
        decimal? priceYearly,
    int maxSessionsPerMonth,
        string featuresJson,
        bool isActive,
        int sortOrder,
  DateTimeOffset createdAt,
      DateTimeOffset updatedAt)
 {
        if (string.IsNullOrWhiteSpace(planCode))
            throw new ArgumentException("Plan code is required.", nameof(planCode));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
  if (maxSessionsPerMonth < 0)
      throw new ArgumentOutOfRangeException(nameof(maxSessionsPerMonth), "Max sessions must be non-negative.");
        if (string.IsNullOrWhiteSpace(featuresJson))
 throw new ArgumentException("Features JSON is required.", nameof(featuresJson));

        Id = id;
 PlanCode = planCode.ToLowerInvariant();
    DisplayName = displayName;
        Description = description;
        PriceMonthly = priceMonthly;
      PriceYearly = priceYearly;
      MaxSessionsPerMonth = maxSessionsPerMonth;
        FeaturesJson = featuresJson;
        IsActive = isActive;
        SortOrder = sortOrder;
      CreatedAt = createdAt;
    UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    /// <summary>
    /// Unique plan identifier: 'free', 'plan-a', 'plan-b'
    /// </summary>
    public string PlanCode { get; private set; }

    /// <summary>
    /// User-facing plan name: 'Free', 'Plan A', 'Plan B'
    /// </summary>
public string DisplayName { get; private set; }

    /// <summary>
/// Marketing description shown on pricing page
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Monthly price in USD
    /// </summary>
    public decimal PriceMonthly { get; private set; }

    /// <summary>
 /// Annual price in USD (optional, for annual billing discount)
    /// </summary>
 public decimal? PriceYearly { get; private set; }

    /// <summary>
    /// Maximum sessions facilitator can create per calendar month
    /// </summary>
    public int MaxSessionsPerMonth { get; private set; }

    /// <summary>
    /// JSON-serialized feature flags: {"aiAssist": true, "fiveWhys": true, "aiSummary": true}
    /// Extensible — new features can be added without schema changes
    /// </summary>
    public string FeaturesJson { get; private set; }

    /// <summary>
    /// Whether this plan is active and visible on pricing page
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Display order on pricing page (lower numbers first)
    /// </summary>
    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(
        string displayName,
        string? description,
        decimal priceMonthly,
     decimal? priceYearly,
     int maxSessionsPerMonth,
     string featuresJson,
        bool isActive,
        int sortOrder,
        DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(displayName))
    throw new ArgumentException("Display name is required.", nameof(displayName));
        if (maxSessionsPerMonth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxSessionsPerMonth));
        if (string.IsNullOrWhiteSpace(featuresJson))
            throw new ArgumentException("Features JSON is required.", nameof(featuresJson));

        DisplayName = displayName;
      Description = description;
        PriceMonthly = priceMonthly;
        PriceYearly = priceYearly;
        MaxSessionsPerMonth = maxSessionsPerMonth;
        FeaturesJson = featuresJson;
        IsActive = isActive;
 SortOrder = sortOrder;
        UpdatedAt = updatedAt;
    }
}
