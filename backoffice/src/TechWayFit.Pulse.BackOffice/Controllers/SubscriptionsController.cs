using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.OperatorOrAbove)]
[Route("subscriptions")]
public class SubscriptionsController : Controller
{
    private readonly IBackOfficeSubscriptionService _subscriptionService;
    private readonly IBackOfficePlanService _planService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
   IBackOfficeSubscriptionService subscriptionService,
  IBackOfficePlanService planService,
     ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _planService = planService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? facilitatorUserId, string? planCode, string? status, int page = 1)
    {
        var query = new SubscriptionSearchQuery(facilitatorUserId, planCode, status, page, 20);
        var result = await _subscriptionService.SearchSubscriptionsAsync(query);
        return View(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var subscription = await _subscriptionService.GetSubscriptionDetailAsync(id);
        if (subscription is null) return NotFound();
        return View(subscription);
    }

    [HttpGet("user/{facilitatorUserId:guid}")]
    public async Task<IActionResult> UserHistory(Guid facilitatorUserId)
    {
        var history = await _subscriptionService.GetUserSubscriptionHistoryAsync(facilitatorUserId);
        return View(history);
    }

    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [HttpGet("assign")]
    public async Task<IActionResult> Assign(Guid? facilitatorUserId)
    {
        var plans = await _planService.SearchPlansAsync(new PlanSearchQuery(true, 1, 100));
        ViewBag.Plans = plans.Items;
        ViewBag.FacilitatorUserId = facilitatorUserId;
        return View();
    }

    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [HttpPost("assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignPlanRequest request)
    {
        if (!ModelState.IsValid)
        {
            var plans = await _planService.SearchPlansAsync(new PlanSearchQuery(true, 1, 100));
            ViewBag.Plans = plans.Items;
            return View(request);
        }

        try
        {
            var operatorId = User.Identity?.Name ?? "unknown";
            var operatorRole = User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var subscription = await _subscriptionService.AssignPlanAsync(request, operatorId, operatorRole, ipAddress);
            TempData["Success"] = $"Plan '{subscription.PlanCode}' assigned successfully.";
            return RedirectToAction(nameof(Detail), new { id = subscription.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var plans = await _planService.SearchPlansAsync(new PlanSearchQuery(true, 1, 100));
            ViewBag.Plans = plans.Items;
            return View(request);
        }
    }

    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [HttpPost("{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string reason)
    {
        try
        {
            var operatorId = User.Identity?.Name ?? "unknown";
            var operatorRole = User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _subscriptionService.CancelSubscriptionAsync(
                   new CancelSubscriptionRequest(id, reason),
                       operatorId, operatorRole, ipAddress);

            TempData["Success"] = "Subscription canceled.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [HttpPost("{id:guid}/reset-quota")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetQuota(Guid id, string reason)
    {
        try
        {
            var operatorId = User.Identity?.Name ?? "unknown";
            var operatorRole = User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _subscriptionService.ResetQuotaAsync(
                            new ResetQuotaRequest(id, reason),
                    operatorId, operatorRole, ipAddress);

            TempData["Success"] = "Quota reset successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
}
