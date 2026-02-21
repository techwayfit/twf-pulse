using TechWayFit.Pulse.BackOffice.Core.Models.Audit;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// Writes and queries the immutable BackOffice audit trail.
/// Every BackOffice write operation MUST call RecordAsync before persisting data.
/// </summary>
public interface IAuditLogService
{
    Task RecordAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<AuditSearchResult> SearchAsync(AuditSearchQuery query, CancellationToken ct = default);
    Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
