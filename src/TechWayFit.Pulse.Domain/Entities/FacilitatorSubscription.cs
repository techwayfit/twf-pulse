using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Tracks a facilitator's subscription to a plan.
/// One active record per user at a time; historical records retained for audit trail.
/// Auto-created with Free plan on first interaction if not exists.
/// </summary>
public sealed class FacilitatorSubscription
{
    public FacilitatorSubscription(
   Guid id,
        Guid facilitatorUserId,
        Guid planId,
        SubscriptionStatus status,
        DateTimeOffset startsAt,
   DateTimeOffset? expiresAt,
        int sessionsUsed,
        DateTimeOffset sessionsResetAt,
        string? paymentProvider,
        string? externalCustomerId,
        string? externalSubscriptionId,
      DateTimeOffset createdAt,
  DateTimeOffset updatedAt)
    {
if (sessionsUsed < 0)
        throw new ArgumentOutOfRangeException(nameof(sessionsUsed), "Sessions used cannot be negative.");

        Id = id;
FacilitatorUserId = facilitatorUserId;
        PlanId = planId;
        Status = status;
        StartsAt = startsAt;
  ExpiresAt = expiresAt;
        CanceledAt = null;
        SessionsUsed = sessionsUsed;
    SessionsResetAt = sessionsResetAt;
   PaymentProvider = paymentProvider;
        ExternalCustomerId = externalCustomerId;
  ExternalSubscriptionId = externalSubscriptionId;
        CreatedAt = createdAt;
     UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    /// <summary>
    /// Facilitator who owns this subscription
    /// </summary>
    public Guid FacilitatorUserId { get; }

    /// <summary>
    /// Plan this subscription is for (Free, Plan A, Plan B, etc.)
    /// </summary>
    public Guid PlanId { get; private set; }

    /// <summary>
    /// Current subscription status
    /// </summary>
    public SubscriptionStatus Status { get; private set; }

  /// <summary>
    /// When subscription started
    /// </summary>
  public DateTimeOffset StartsAt { get; }

    /// <summary>
    /// When subscription expires (null = monthly rolling, no expiry)
    /// </summary>
 public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// When subscription was canceled (null if active)
    /// </summary>
    public DateTimeOffset? CanceledAt { get; private set; }

    /// <summary>
    /// How many sessions consumed in current month
    /// Resets to 0 on SessionsResetAt date
    /// </summary>
    public int SessionsUsed { get; private set; }

    /// <summary>
    /// Next date when SessionsUsed resets to 0
    /// Typically 1st of next month
    /// </summary>
    public DateTimeOffset SessionsResetAt { get; private set; }

    /// <summary>
    /// Payment provider: 'paddle', 'stripe', null (operator-assigned free)
    /// </summary>
    public string? PaymentProvider { get; private set; }

    /// <summary>
    /// External payment provider customer ID (e.g., Paddle customer ID)
    /// </summary>
    public string? ExternalCustomerId { get; private set; }

    /// <summary>
 /// External payment provider subscription ID (e.g., Paddle subscription ID)
    /// </summary>
    public string? ExternalSubscriptionId { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Consume one session from monthly quota
    /// </summary>
    public void ConsumeSession(DateTimeOffset now)
    {
        SessionsUsed++;
        UpdatedAt = now;
  }

    /// <summary>
    /// Reset monthly session quota (called automatically by PlanService)
    /// </summary>
    public void ResetMonthlyQuota(DateTimeOffset now, DateTimeOffset nextResetDate)
    {
      SessionsUsed = 0;
        SessionsResetAt = nextResetDate;
        UpdatedAt = now;
    }

    /// <summary>
  /// Upgrade to a different plan (preserves payment info)
    /// </summary>
    public void UpgradePlan(Guid newPlanId, DateTimeOffset now)
    {
   PlanId = newPlanId;
        UpdatedAt = now;
    }

    /// <summary>
    /// Cancel subscription (marks for cancellation, remains active until expiry)
    /// </summary>
    public void Cancel(DateTimeOffset canceledAt, DateTimeOffset expiresAt)
    {
        Status = SubscriptionStatus.Canceled;
        CanceledAt = canceledAt;
    ExpiresAt = expiresAt;
  UpdatedAt = canceledAt;
    }

  /// <summary>
  /// Mark subscription as expired (called by background job)
    /// </summary>
    public void MarkExpired(DateTimeOffset now)
    {
        Status = SubscriptionStatus.Expired;
     UpdatedAt = now;
    }

/// <summary>
  /// Set payment provider information (called by webhook after successful payment)
    /// </summary>
    public void SetPaymentInfo(
        string paymentProvider,
    string externalCustomerId,
        string externalSubscriptionId,
 DateTimeOffset now)
{
     PaymentProvider = paymentProvider ?? throw new ArgumentNullException(nameof(paymentProvider));
        ExternalCustomerId = externalCustomerId ?? throw new ArgumentNullException(nameof(externalCustomerId));
        ExternalSubscriptionId = externalSubscriptionId ?? throw new ArgumentNullException(nameof(externalSubscriptionId));
    UpdatedAt = now;
  }
}
