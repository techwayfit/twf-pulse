using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly BackOfficeDbContext _db;

    public AuditLogService(BackOfficeDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        var record = new AuditLogRecord
        {
            Id          = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            OperatorId  = entry.OperatorId,
            OperatorRole = entry.OperatorRole,
            Action      = entry.Action,
            EntityType  = entry.EntityType,
            EntityId    = entry.EntityId,
            FieldName   = entry.FieldName,
            OldValue    = entry.OldValue,
            NewValue    = entry.NewValue,
            Reason      = entry.Reason,
            IpAddress   = entry.IpAddress,
            OccurredAt  = entry.OccurredAt
        };

        _db.AuditLogs.Add(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AuditSearchResult> SearchAsync(AuditSearchQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.OperatorId))
            q = q.Where(x => x.OperatorId == query.OperatorId);

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(x => x.EntityType == query.EntityType);

        if (!string.IsNullOrWhiteSpace(query.EntityId))
            q = q.Where(x => x.EntityId == query.EntityId);

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(x => x.Action.Contains(query.Action));

        if (query.From.HasValue)
            q = q.Where(x => x.OccurredAt >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(x => x.OccurredAt <= query.To.Value);

        var totalCount = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AuditLogEntry(
                x.Id, x.OperatorId, x.OperatorRole, x.Action,
                x.EntityType, x.EntityId, x.FieldName,
                x.OldValue, x.NewValue, x.Reason,
                x.IpAddress, x.OccurredAt))
            .ToListAsync(ct);

        return new AuditSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var x = await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (x is null) return null;
        return new AuditLogEntry(x.Id, x.OperatorId, x.OperatorRole, x.Action,
            x.EntityType, x.EntityId, x.FieldName,
            x.OldValue, x.NewValue, x.Reason,
            x.IpAddress, x.OccurredAt);
    }
}
