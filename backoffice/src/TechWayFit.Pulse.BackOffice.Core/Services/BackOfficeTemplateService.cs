using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Models.Templates;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficeTemplateService : IBackOfficeTemplateService
{
    private readonly BackOfficeDbContext _db;
    private readonly IAuditLogService _audit;

    public BackOfficeTemplateService(BackOfficeDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<TemplateSearchResult> SearchAsync(TemplateSearchQuery query, CancellationToken ct = default)
    {
        var q = _db.SessionTemplates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.NameContains))
            q = q.Where(t => t.Name.Contains(query.NameContains));

        if (query.Category.HasValue)
            q = q.Where(t => t.Category == (int)query.Category.Value);

        if (query.IsSystem.HasValue)
            q = q.Where(t => t.IsSystemTemplate == query.IsSystem.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TemplateSummary(
                t.Id,
                t.Name,
                t.Description,
                (TemplateCategory)t.Category,
                t.IconEmoji,
                t.IsSystemTemplate,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(ct);

        return new TemplateSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<TemplateDetailViewModel?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.SessionTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (t is null) return null;

        return new TemplateDetailViewModel(
            t.Id,
            t.Name,
            t.Description,
            (TemplateCategory)t.Category,
            t.IconEmoji,
            t.ConfigJson,
            t.IsSystemTemplate,
            t.CreatedByUserId,
            t.CreatedAt,
            t.UpdatedAt);
    }

    public async Task<Guid> CreateAsync(SaveTemplateRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var record = new SessionTemplateRecord
        {
            Id = id,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = (int)request.Category,
            IconEmoji = request.IconEmoji.Trim(),
            ConfigJson = request.ConfigJson,
            IsSystemTemplate = false,
            CreatedByUserId = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.SessionTemplates.Add(record);

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "Template.Create", "SessionTemplate", id.ToString(),
            null, null, request.Name,
            null, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        return id;
    }

    public async Task UpdateAsync(Guid id, SaveTemplateRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var record = await _db.SessionTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Template {id} not found.");

        record.Name        = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.Category    = (int)request.Category;
        record.IconEmoji   = request.IconEmoji.Trim();
        record.ConfigJson  = request.ConfigJson;
        record.UpdatedAt   = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "Template.Update", "SessionTemplate", id.ToString(),
            null, null, request.Name,
            null, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var record = await _db.SessionTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Template {id} not found.");

        var name = record.Name;
        _db.SessionTemplates.Remove(record);

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "Template.Delete", "SessionTemplate", id.ToString(),
            null, name, null,
            null, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }
}
