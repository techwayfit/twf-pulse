using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.OperatorOrAbove)]
public sealed class AuditController : Controller
{
    private readonly IAuditLogService _auditService;

    public AuditController(IAuditLogService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? operatorId, string? entityType, string? entityId, string? action,
        DateTimeOffset? from, DateTimeOffset? to, int page = 1)
    {
        var query  = new AuditSearchQuery(operatorId, entityType, entityId, action, from, to, page, 50);
        var result = await _auditService.SearchAsync(query);
        ViewBag.Query = query;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var entry = await _auditService.GetByIdAsync(id);
        if (entry is null) return NotFound();
        return View(entry);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? operatorId, string? entityType, string? entityId, string? action,
        DateTimeOffset? from, DateTimeOffset? to)
    {
        // Fetch up to 10 000 rows for export
        var result = await _auditService.SearchAsync(
            new AuditSearchQuery(operatorId, entityType, entityId, action, from, to, 1, 10_000));

        var csv = BuildCsv(result.Items);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"audit-export-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static string BuildCsv(IEnumerable<AuditLogEntry> items)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,OccurredAt,OperatorId,OperatorRole,Action,EntityType,EntityId,FieldName,OldValue,NewValue,Reason,IpAddress");
        foreach (var e in items)
        {
            sb.AppendLine(string.Join(",",
                e.Id, e.OccurredAt.ToString("O"), Csv(e.OperatorId), Csv(e.OperatorRole),
                Csv(e.Action), Csv(e.EntityType), Csv(e.EntityId),
                Csv(e.FieldName), Csv(e.OldValue), Csv(e.NewValue),
                Csv(e.Reason), Csv(e.IpAddress)));
        }
        return sb.ToString();

        static string Csv(string? v) =>
            v is null ? "" : $"\"{v.Replace("\"", "\"\"")}\"";
    }
}
