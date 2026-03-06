namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Audit record for promo code redemptions.
/// Tracks who redeemed which code and when, for analytics and fraud prevention.
/// </summary>
public sealed class PromoCodeRedemption
{
    public PromoCodeRedemption(
        Guid id,
        Guid promoCodeId,
  Guid facilitatorUserId,
    Guid subscriptionId,
        DateTimeOffset redeemedAt,
  string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
          throw new ArgumentException("IP address is required.", nameof(ipAddress));

        Id = id;
 PromoCodeId = promoCodeId;
        FacilitatorUserId = facilitatorUserId;
        SubscriptionId = subscriptionId;
  RedeemedAt = redeemedAt;
        IpAddress = ipAddress;
    }

  public Guid Id { get; }

    /// <summary>
    /// Promo code that was redeemed
    /// </summary>
    public Guid PromoCodeId { get; }

    /// <summary>
    /// User who redeemed the code
    /// </summary>
    public Guid FacilitatorUserId { get; }

    /// <summary>
 /// Subscription created by this redemption
    /// </summary>
    public Guid SubscriptionId { get; }

    /// <summary>
    /// When the code was redeemed
    /// </summary>
    public DateTimeOffset RedeemedAt { get; }

    /// <summary>
    /// IP address of the user who redeemed the code (for fraud detection)
    /// </summary>
    public string IpAddress { get; }
}
