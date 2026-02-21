using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Auth;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.SuperAdminOnly)]
public sealed class BackOfficeUsersController : Controller
{
    private readonly IBackOfficeAuthService _authService;

    public BackOfficeUsersController(IBackOfficeAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var operators = await _authService.ListOperatorsAsync();
        return View(operators);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateBackOfficeUserRequest(string.Empty, string.Empty, "Operator"));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBackOfficeUserRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        await _authService.CreateOperatorAsync(request, User.Identity!.Name!);
        TempData["Success"] = $"Operator '{request.Username}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(UpdateBackOfficeUserRoleRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _authService.UpdateRoleAsync(request, User.Identity!.Name!, ip);
        TempData["Success"] = "Role updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(ToggleBackOfficeUserActiveRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _authService.ToggleActiveAsync(request, User.Identity!.Name!, ip);
        TempData["Success"] = request.IsActive ? "Operator activated." : "Operator deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
