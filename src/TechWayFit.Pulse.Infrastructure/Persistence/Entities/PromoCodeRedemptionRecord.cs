namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core record for PromoCodeRedemption entity
/// </summary>
public sealed class PromoCodeRedemptionRecord
{
    public required Guid Id { get; init; }
    public required Guid PromoCodeId { get; init; }
    public required Guid FacilitatorUserId { get; init; }
  public required Guid SubscriptionId { get; init; }
    public required DateTimeOffset RedeemedAt { get; init; }
    public required string IpAddress { get; init; }
}
