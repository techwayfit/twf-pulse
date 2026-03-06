namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core record for FacilitatorSubscription entity
/// </summary>
public sealed class FacilitatorSubscriptionRecord
{
    public required Guid Id { get; init; }
   public required Guid FacilitatorUserId { get; init; }
    public required Guid PlanId { get; set; }
    public required string Status { get; set; } // Active, Canceled, Expired, Trial
  public required DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? CanceledAt { get; set; }
    public required int SessionsUsed { get; set; }
    public required DateTimeOffset SessionsResetAt { get; set; }
    public string? PaymentProvider { get; set; }
    public string? ExternalCustomerId { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
