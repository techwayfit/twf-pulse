using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.OperatorOrAbove)]
public sealed class DashboardController : Controller
{
    private readonly BackOfficeDbContext _db;

    public DashboardController(BackOfficeDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.UserCount       = await _db.FacilitatorUsers.CountAsync();
        ViewBag.SessionCount    = await _db.Sessions.CountAsync();
        ViewBag.ParticipantCount = await _db.Participants.CountAsync();
        ViewBag.AuditLogCount   = await _db.AuditLogs.CountAsync();
        return View();
    }
}
