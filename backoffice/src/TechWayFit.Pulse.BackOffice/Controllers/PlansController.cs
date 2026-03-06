using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.SuperAdminOnly)]
[Route("plans")]
public class PlansController : Controller
{
    private readonly IBackOfficePlanService _planService;
    private readonly ILogger<PlansController> _logger;

    public PlansController(IBackOfficePlanService planService, ILogger<PlansController> logger)
    {
     _planService = planService;
    _logger = logger;
 }

    [HttpGet]
    public async Task<IActionResult> Index(bool? isActive, int page = 1)
    {
  var query = new PlanSearchQuery(isActive, page, 20);
   var result = await _planService.SearchPlansAsync(query);
        return View(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
 {
     var plan = await _planService.GetPlanDetailAsync(id);
   if (plan is null) return NotFound();
   return View(plan);
    }

  [HttpGet("create")]
    public IActionResult Create()
    {
  return View();
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSubscriptionPlanRequest request)
    {
  if (!ModelState.IsValid) return View(request);

     try
        {
       var operatorId = User.Identity?.Name ?? "unknown";
       var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

     var plan = await _planService.CreatePlanAsync(request, operatorId, "SuperAdmin", ipAddress);
      TempData["Success"] = $"Plan '{plan.PlanCode}' created successfully.";
      return RedirectToAction(nameof(Detail), new { id = plan.Id });
   }
     catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
 return View(request);
}
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
 {
        var plan = await _planService.GetPlanDetailAsync(id);
  if (plan is null) return NotFound();
  return View(plan);
    }

    [HttpPost("{id:guid}/edit")]
  [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateSubscriptionPlanRequest request)
    {
   if (!ModelState.IsValid) return View(await _planService.GetPlanDetailAsync(id));

   try
  {
   var operatorId = User.Identity?.Name ?? "unknown";
   var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

       await _planService.UpdatePlanAsync(request, operatorId, "SuperAdmin", ipAddress);
          TempData["Success"] = "Plan updated successfully.";
     return RedirectToAction(nameof(Detail), new { id });
    }
  catch (Exception ex)
        {
       ModelState.AddModelError("", ex.Message);
          return View(await _planService.GetPlanDetailAsync(id));
    }
    }

    [HttpPost("{id:guid}/toggle-active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive, string reason)
    {
        try
 {
            var operatorId = User.Identity?.Name ?? "unknown";
  var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _planService.TogglePlanActiveAsync(id, isActive, reason, operatorId, "SuperAdmin", ipAddress);
   TempData["Success"] = isActive ? "Plan activated." : "Plan deactivated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
 }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue()
    {
   var summary = await _planService.GetRevenueSummaryAsync();
      return View(summary);
    }
}
