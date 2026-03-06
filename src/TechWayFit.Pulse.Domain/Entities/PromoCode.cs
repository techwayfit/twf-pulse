namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Represents a promotional code that grants temporary access to a specific plan.
/// Operators create promo codes for marketing campaigns, partnerships, or comps.
/// </summary>
public sealed class PromoCode
{
    public PromoCode(
        Guid id,
        string code,
        Guid targetPlanId,
        int durationDays,
        int? maxRedemptions,
  int redemptionsUsed,
        DateTimeOffset validFrom,
        DateTimeOffset validUntil,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
  {
        if (string.IsNullOrWhiteSpace(code))
      throw new ArgumentException("Promo code is required.", nameof(code));
        if (durationDays <= 0)
      throw new ArgumentOutOfRangeException(nameof(durationDays), "Duration must be positive.");
        if (redemptionsUsed < 0)
  throw new ArgumentOutOfRangeException(nameof(redemptionsUsed), "Redemptions used cannot be negative.");
        if (maxRedemptions.HasValue && maxRedemptions.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRedemptions), "Max redemptions must be positive.");
     if (validFrom >= validUntil)
         throw new ArgumentException("Valid from date must be before valid until date.");

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        TargetPlanId = targetPlanId;
        DurationDays = durationDays;
 MaxRedemptions = maxRedemptions;
        RedemptionsUsed = redemptionsUsed;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        IsActive = isActive;
     CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    /// <summary>
    /// Unique promotional code (e.g., "LAUNCH2025", "FRIENDS50")
    /// Stored uppercase for case-insensitive matching
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
 /// Plan to assign when this code is redeemed (e.g., Plan A, Plan B)
    /// </summary>
    public Guid TargetPlanId { get; private set; }

    /// <summary>
    /// Number of days the promotional subscription lasts
    /// </summary>
    public int DurationDays { get; private set; }

    /// <summary>
  /// Maximum number of times this code can be redeemed (null = unlimited)
    /// </summary>
    public int? MaxRedemptions { get; private set; }

  /// <summary>
    /// Number of times this code has been redeemed
    /// </summary>
    public int RedemptionsUsed { get; private set; }

    /// <summary>
    /// Date when this promo code becomes valid
    /// </summary>
public DateTimeOffset ValidFrom { get; private set; }

 /// <summary>
    /// Date when this promo code expires
    /// </summary>
    public DateTimeOffset ValidUntil { get; private set; }

    /// <summary>
    /// Whether this promo code is active (can be redeemed)
    /// </summary>
    public bool IsActive { get; private set; }

 public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Increment redemption counter when code is used
    /// </summary>
 public void IncrementRedemptions(DateTimeOffset now)
    {
        RedemptionsUsed++;
        UpdatedAt = now;
    }

    /// <summary>
    /// Update promo code configuration
    /// </summary>
    public void Update(
        string code,
        Guid targetPlanId,
  int durationDays,
        int? maxRedemptions,
        DateTimeOffset validFrom,
        DateTimeOffset validUntil,
        bool isActive,
      DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
          throw new ArgumentException("Promo code is required.", nameof(code));
 if (durationDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationDays), "Duration must be positive.");
        if (maxRedemptions.HasValue && maxRedemptions.Value <= 0)
          throw new ArgumentOutOfRangeException(nameof(maxRedemptions), "Max redemptions must be positive.");
        if (validFrom >= validUntil)
  throw new ArgumentException("Valid from date must be before valid until date.");

        Code = code.Trim().ToUpperInvariant();
     TargetPlanId = targetPlanId;
        DurationDays = durationDays;
        MaxRedemptions = maxRedemptions;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        IsActive = isActive;
    UpdatedAt = now;
    }

 /// <summary>
    /// Toggle active status
    /// </summary>
    public void ToggleActive(bool isActive, DateTimeOffset now)
    {
      IsActive = isActive;
        UpdatedAt = now;
    }

    /// <summary>
    /// Check if code is currently valid (ignoring active status)
    /// </summary>
 public bool IsValidForPeriod(DateTimeOffset now)
    {
        return now >= ValidFrom && now <= ValidUntil;
 }

    /// <summary>
    /// Check if code has remaining redemptions
    /// </summary>
    public bool HasRedemptionsRemaining()
    {
        return !MaxRedemptions.HasValue || RedemptionsUsed < MaxRedemptions.Value;
    }
}
