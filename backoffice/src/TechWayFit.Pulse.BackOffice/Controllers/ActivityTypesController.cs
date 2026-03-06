using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.SuperAdminOnly)]
[Route("activity-types")]
public class ActivityTypesController : Controller
{
    private readonly IBackOfficeActivityTypeService _activityTypeService;
    private readonly IBackOfficePlanService _planService;
    private readonly ILogger<ActivityTypesController> _logger;

    public ActivityTypesController(
   IBackOfficeActivityTypeService activityTypeService,
        IBackOfficePlanService planService,
      ILogger<ActivityTypesController> logger)
    {
        _activityTypeService = activityTypeService;
        _planService = planService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool? isActive, bool? requiresPremium, int page = 1)
    {
  var query = new ActivityTypeSearchQuery(isActive, requiresPremium, page, 50);
  var result = await _activityTypeService.SearchActivityTypesAsync(query);
  return View(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
   var activityType = await _activityTypeService.GetActivityTypeDetailAsync(id);
   if (activityType is null) return NotFound();
        return View(activityType);
    }

    [HttpGet("create")]
 public async Task<IActionResult> Create()
 {
     // Load active plans for the plan selector
        var plans = await _planService.GetAllActivePlansAsync();
     ViewBag.AvailablePlans = plans;
        
   return View();
 }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateActivityTypeDefinitionRequest request)
    {
  if (!ModelState.IsValid) return View(request);

 try
 {
       var operatorId = User.Identity?.Name ?? "unknown";
 var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

 var activityType = await _activityTypeService.CreateActivityTypeAsync(
    request, operatorId, "SuperAdmin", ipAddress);

       TempData["Success"] = $"Activity type '{activityType.DisplayName}' created successfully.";
  return RedirectToAction(nameof(Detail), new { id = activityType.Id });
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
        var activityType = await _activityTypeService.GetActivityTypeDetailAsync(id);
     if (activityType is null) return NotFound();
        
        // Load active plans for the plan selector
 var plans = await _planService.GetAllActivePlansAsync();
     ViewBag.AvailablePlans = plans;
        
        return View(activityType);
}

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(Guid id, UpdateActivityTypeDefinitionRequest request)
{
  if (!ModelState.IsValid) return View(await _activityTypeService.GetActivityTypeDetailAsync(id));

      try
{
   var operatorId = User.Identity?.Name ?? "unknown";
   var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

  await _activityTypeService.UpdateActivityTypeAsync(request, operatorId, "SuperAdmin", ipAddress);
   TempData["Success"] = "Activity type updated successfully.";
  return RedirectToAction(nameof(Detail), new { id });
  }
  catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
     return View(await _activityTypeService.GetActivityTypeDetailAsync(id));
   }
    }

    [HttpPost("{id:guid}/toggle-premium")]
    [ValidateAntiForgeryToken]
 public async Task<IActionResult> TogglePremium(Guid id, bool requiresPremium, string? applicablePlanIds, bool isAvailableToAllPlans, string reason)
    {
   try
    {
            var operatorId = User.Identity?.Name ?? "unknown";
 var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

     await _activityTypeService.TogglePremiumAsync(
    new TogglePremiumRequest(id, requiresPremium, applicablePlanIds, isAvailableToAllPlans, reason),
        operatorId, "SuperAdmin", ipAddress);

  TempData["Success"] = requiresPremium ? "Activity type made premium." : "Activity type made free.";
        }
   catch (Exception ex)
        {
TempData["Error"] = ex.Message;
 }

 return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:guid}/toggle-active")]
 [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive, string reason)
    {
        try
  {
       var operatorId = User.Identity?.Name ?? "unknown";
 var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

       await _activityTypeService.ToggleActiveAsync(id, isActive, reason, operatorId, "SuperAdmin", ipAddress);
      TempData["Success"] = isActive ? "Activity type activated." : "Activity type deactivated.";
}
  catch (Exception ex)
        {
TempData["Error"] = ex.Message;
  }

  return RedirectToAction(nameof(Detail), new { id });
    }

  [HttpPost("reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromBody] ReorderRequest request)
    {
        try
  {
      var operatorId = User.Identity?.Name ?? "unknown";
   var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

 await _activityTypeService.ReorderActivityTypesAsync(
    request.Items, request.Reason, operatorId, "SuperAdmin", ipAddress);

         return Json(new { success = true });
        }
        catch (Exception ex)
        {
return Json(new { success = false, error = ex.Message });
    }
    }

    public sealed record ReorderRequest(IReadOnlyList<(Guid Id, int NewSortOrder)> Items, string Reason);
}
