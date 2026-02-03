namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

public sealed class FacilitatorUserDataRecord
{
    public Guid Id { get; set; }
    
    public Guid FacilitatorUserId { get; set; }
    
    public string Key { get; set; } = string.Empty;
    
    public string Value { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
}
