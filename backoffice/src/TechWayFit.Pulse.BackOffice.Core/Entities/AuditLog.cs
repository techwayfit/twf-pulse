namespace TechWayFit.Pulse.BackOffice.Core.Entities;

/// <summary>
/// Immutable audit record written for every BackOffice write operation.
/// Never updated or deleted — the audit trail is permanent.
/// </summary>
public sealed class AuditLog
{
    public AuditLog(
        Guid id,
        string operatorId,
        string operatorRole,
        string action,
        string entityType,
        string entityId,
        string? fieldName,
        string? oldValue,
        string? newValue,
        string? reason,
        string ipAddress,
        DateTimeOffset occurredAt)
    {
        Id = id;
        OperatorId = operatorId.Trim();
        OperatorRole = operatorRole.Trim();
        Action = action.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId.Trim();
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
        Reason = reason;
        IpAddress = ipAddress.Trim();
        OccurredAt = occurredAt;
    }

    public Guid Id { get; }

    /// <summary>BackOffice operator username who performed the action.</summary>
    public string OperatorId { get; }

    /// <summary>Role of the operator at time of action (Operator | SuperAdmin).</summary>
    public string OperatorRole { get; }

    /// <summary>Descriptive action name, e.g. "DisableUser" or "ForceEndSession".</summary>
    public string Action { get; }

    /// <summary>Domain entity type affected, e.g. "FacilitatorUser" or "Session".</summary>
    public string EntityType { get; }

    /// <summary>String representation of the affected record's primary key.</summary>
    public string EntityId { get; }

    /// <summary>Specific field changed, if applicable.</summary>
    public string? FieldName { get; }

    /// <summary>Serialised previous value (JSON or plain string).</summary>
    public string? OldValue { get; }

    /// <summary>Serialised new value (JSON or plain string).</summary>
    public string? NewValue { get; }

    /// <summary>Operator-supplied justification for the change.</summary>
    public string? Reason { get; }

    /// <summary>Client IP address captured for accountability.</summary>
    public string IpAddress { get; }

    public DateTimeOffset OccurredAt { get; }
}
