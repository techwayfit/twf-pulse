namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core record for ActivityTypeDefinition entity
/// </summary>
public sealed class ActivityTypeDefinitionRecord
{
    public required Guid Id { get; init; }
    public required int ActivityType { get; init; } // Maps to ActivityType enum
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string IconClass { get; set; }
    public required string ColorHex { get; set; }
    public required bool RequiresPremium { get; set; }
    public string? MinPlanCode { get; set; }
    public required bool IsActive { get; set; }
  public required int SortOrder { get; set; }
  public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
