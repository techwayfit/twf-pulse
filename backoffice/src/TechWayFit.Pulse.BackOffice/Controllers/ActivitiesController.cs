using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Sessions;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.OperatorOrAbove)]
public sealed class ActivitiesController : Controller
{
    private readonly IBackOfficeSessionService _sessionService;

    public ActivitiesController(IBackOfficeSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var detail = await _sessionService.GetActivityDetailAsync(id);
        if (detail is null) return NotFound();
        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    public async Task<IActionResult> UpdateConfig(Guid id, string configJson, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Reason is required.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var (operatorId, role, ip) = OperatorContext();

        try
        {
            // Validate JSON before saving
            System.Text.Json.JsonDocument.Parse(configJson);
        }
        catch
        {
            TempData["Error"] = "Invalid JSON — please fix syntax errors before saving.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        await _sessionService.UpdateActivityConfigAsync(
            new UpdateActivityConfigRequest(id, configJson, reason),
            operatorId, role, ip);

        TempData["Success"] = "Activity config updated.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private (string OperatorId, string Role, string Ip) OperatorContext() =>
        (User.Identity!.Name!,
         User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator",
         HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
}
