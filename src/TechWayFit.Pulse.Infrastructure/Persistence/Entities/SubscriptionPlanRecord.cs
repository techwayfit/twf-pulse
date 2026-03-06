namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core record for SubscriptionPlan entity
/// </summary>
public sealed class SubscriptionPlanRecord
{
    public required Guid Id { get; init; }
    public required string PlanCode { get; set; }
 public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required decimal PriceMonthly { get; set; }
    public decimal? PriceYearly { get; set; }
    public required int MaxSessionsPerMonth { get; set; }
public required string FeaturesJson { get; set; }
    public required bool IsActive { get; set; }
    public required int SortOrder { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
