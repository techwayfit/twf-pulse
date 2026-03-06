namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core record for PromoCode entity
/// </summary>
public sealed class PromoCodeRecord
{
    public required Guid Id { get; init; }
 public required string Code { get; set; }
    public required Guid TargetPlanId { get; set; }
  public required int DurationDays { get; set; }
    public int? MaxRedemptions { get; set; }
    public required int RedemptionsUsed { get; set; }
    public required DateTimeOffset ValidFrom { get; set; }
    public required DateTimeOffset ValidUntil { get; set; }
    public required bool IsActive { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
