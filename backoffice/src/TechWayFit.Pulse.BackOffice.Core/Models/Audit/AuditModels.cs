namespace TechWayFit.Pulse.BackOffice.Core.Models.Audit;

public record AuditLogEntry(
    Guid Id,
    string OperatorId,
    string OperatorRole,
    string Action,
    string EntityType,
    string EntityId,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    string? Reason,
    string IpAddress,
    DateTimeOffset OccurredAt);

public record AuditSearchQuery(
    string? OperatorId,
    string? EntityType,
    string? EntityId,
    string? Action,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 50);

public record AuditSearchResult(
    IReadOnlyList<AuditLogEntry> Items,
    int TotalCount,
    int Page,
    int PageSize);
