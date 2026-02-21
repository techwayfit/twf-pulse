namespace TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;

public sealed class AuditLogRecord
{
    public Guid Id { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
