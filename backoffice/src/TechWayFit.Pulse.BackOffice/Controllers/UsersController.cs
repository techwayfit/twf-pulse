using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Users;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.OperatorOrAbove)]
public sealed class UsersController : Controller
{
    private readonly IBackOfficeUserService _userService;

    public UsersController(IBackOfficeUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? email, string? name, bool? disabled, int page = 1)
    {
        var query  = new UserSearchQuery(email, name, disabled, page, 30);
        var result = await _userService.SearchAsync(query);
        ViewBag.Query = query;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var detail = await _userService.GetDetailAsync(id);
        if (detail is null) return NotFound();
        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDisabled(Guid id, bool disable, string reason)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var operatorId   = User.Identity!.Name!;
        var operatorRole = User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator";

        await _userService.SetDisabledAsync(
            new DisableUserRequest(id, disable, reason),
            operatorId, operatorRole, ip);

        TempData["Success"] = $"User account {(disable ? "disabled" : "enabled")} successfully.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDisplayName(Guid id, string newDisplayName, string reason)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var operatorId   = User.Identity!.Name!;
        var operatorRole = User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator";

        await _userService.UpdateDisplayNameAsync(
            new UpdateUserDisplayNameRequest(id, newDisplayName, reason),
            operatorId, operatorRole, ip);

        TempData["Success"] = "Display name updated.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    public async Task<IActionResult> UpdateEmail(Guid id, string newEmail, string reason)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _userService.UpdateEmailAsync(
            new UpdateUserEmailRequest(id, newEmail, reason),
            User.Identity!.Name!, "SuperAdmin", ip);

        TempData["Success"] = "Email address updated.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    public async Task<IActionResult> Delete(Guid id, string confirmationEmail, string reason)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _userService.DeleteUserAsync(
            id, confirmationEmail, reason,
            User.Identity!.Name!, "SuperAdmin", ip);

        TempData["Success"] = "User deleted permanently.";
        return RedirectToAction(nameof(Index));
    }
}
