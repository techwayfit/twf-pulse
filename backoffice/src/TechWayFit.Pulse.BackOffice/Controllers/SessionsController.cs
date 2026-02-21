using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Sessions;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.OperatorOrAbove)]
public sealed class SessionsController : Controller
{
    private readonly IBackOfficeSessionService _sessionService;

    public SessionsController(IBackOfficeSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? code, string? title, SessionStatus? status, Guid? ownerId, int page = 1)
    {
        var query  = new SessionSearchQuery(code, title, status, ownerId, page, 30);
        var result = await _sessionService.SearchAsync(query);
        ViewBag.Query      = query;
        ViewBag.StatusList = Enum.GetValues<SessionStatus>();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var detail = await _sessionService.GetDetailAsync(id);
        if (detail is null) return NotFound();
        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceEnd(Guid id, string reason)
    {
        var (operatorId, role, ip) = OperatorContext();
        await _sessionService.ForceEndAsync(new ForceEndSessionRequest(id, reason), operatorId, role, ip);
        TempData["Success"] = "Session force-ended.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtendExpiry(Guid id, int additionalDays, string reason)
    {
        var (operatorId, role, ip) = OperatorContext();
        await _sessionService.ExtendExpiryAsync(new ExtendSessionExpiryRequest(id, additionalDays, reason), operatorId, role, ip);
        TempData["Success"] = $"Expiry extended by {additionalDays} day(s).";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLock(Guid id, bool locked, string reason)
    {
        var (operatorId, role, ip) = OperatorContext();
        await _sessionService.SetLockAsync(new LockSessionRequest(id, locked, reason), operatorId, role, ip);
        TempData["Success"] = locked ? "Session locked." : "Session unlocked.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    public async Task<IActionResult> Delete(Guid id, string confirmationCode, string reason)
    {
        var (operatorId, role, ip) = OperatorContext();
        await _sessionService.DeleteSessionAsync(new DeleteSessionRequest(id, confirmationCode, reason), operatorId, role, ip);
        TempData["Success"] = "Session permanently deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceCloseActivity(Guid activityId, Guid sessionId, string reason)
    {
        var (operatorId, role, ip) = OperatorContext();
        await _sessionService.ForceCloseActivityAsync(activityId, reason, operatorId, role, ip);
        TempData["Success"] = "Activity force-closed.";
        return RedirectToAction(nameof(Detail), new { id = sessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    public async Task<IActionResult> RemoveParticipant(Guid participantId, Guid sessionId, string reason)
    {
        var (operatorId, role, ip) = OperatorContext();
        await _sessionService.RemoveParticipantAsync(participantId, reason, operatorId, role, ip);
        TempData["Success"] = "Participant removed.";
        return RedirectToAction(nameof(Detail), new { id = sessionId });
    }

    private (string OperatorId, string Role, string Ip) OperatorContext() =>
        (User.Identity!.Name!,
         User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator",
         HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
}
