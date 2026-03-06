using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.SuperAdminOnly)]
[Route("promo-codes")]
public class PromoCodesController : Controller
{
    private readonly IBackOfficePromoCodeService _promoCodeService;
    private readonly IBackOfficePlanService _planService;
    private readonly ILogger<PromoCodesController> _logger;

    public PromoCodesController(
        IBackOfficePromoCodeService promoCodeService,
  IBackOfficePlanService planService,
        ILogger<PromoCodesController> logger)
    {
        _promoCodeService = promoCodeService;
   _planService = planService;
   _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool? isActive, bool? isExpired, int page = 1)
 {
        var query = new PromoCodeSearchQuery(isActive, isExpired, page, 20);
  var result = await _promoCodeService.SearchPromoCodesAsync(query);
    return View(result);
    }

    [HttpGet("{id:guid}")]
 public async Task<IActionResult> Detail(Guid id)
    {
        var promo = await _promoCodeService.GetPromoCodeDetailAsync(id);
        if (promo is null) return NotFound();
        
        // Get redemption stats
        var (totalRedemptions, uniqueUsers) = await _promoCodeService.GetRedemptionStatsAsync(id);
        ViewBag.TotalRedemptions = totalRedemptions;
     ViewBag.UniqueUsers = uniqueUsers;
        
   return View(promo);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Plans = await _planService.GetAllActivePlansAsync();
    return View();
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePromoCodeRequest request)
    {
      if (!ModelState.IsValid)
        {
ViewBag.Plans = await _planService.GetAllActivePlansAsync();
 return View(request);
        }

   try
        {
            var operatorId = User.Identity?.Name ?? "unknown";
  var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

var promo = await _promoCodeService.CreatePromoCodeAsync(request, operatorId, "SuperAdmin", ipAddress);
            TempData["Success"] = $"Promo code '{promo.Code}' created successfully.";
     return RedirectToAction(nameof(Detail), new { id = promo.Id });
        }
      catch (InvalidOperationException ex)
        {
          ModelState.AddModelError("", ex.Message);
       ViewBag.Plans = await _planService.GetAllActivePlansAsync();
      return View(request);
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var promo = await _promoCodeService.GetPromoCodeDetailAsync(id);
        if (promo is null) return NotFound();
        
        ViewBag.Plans = await _planService.GetAllActivePlansAsync();
        return View(promo);
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdatePromoCodeRequest request)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Plans = await _planService.GetAllActivePlansAsync();
   return View(await _promoCodeService.GetPromoCodeDetailAsync(id));
        }

        try
        {
            var operatorId = User.Identity?.Name ?? "unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _promoCodeService.UpdatePromoCodeAsync(request, operatorId, "SuperAdmin", ipAddress);
   TempData["Success"] = "Promo code updated successfully.";
     return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
       ViewBag.Plans = await _planService.GetAllActivePlansAsync();
  return View(await _promoCodeService.GetPromoCodeDetailAsync(id));
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

  await _promoCodeService.TogglePromoCodeActiveAsync(id, isActive, reason, operatorId, "SuperAdmin", ipAddress);
TempData["Success"] = isActive ? "Promo code activated." : "Promo code deactivated.";
      }
        catch (Exception ex)
   {
      TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:guid}/delete")]
  [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string reason)
    {
        try
        {
   var operatorId = User.Identity?.Name ?? "unknown";
         var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _promoCodeService.DeletePromoCodeAsync(id, reason, operatorId, "SuperAdmin", ipAddress);
     TempData["Success"] = "Promo code deleted successfully.";
    return RedirectToAction(nameof(Index));
 }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }
    }

    [HttpGet("{id:guid}/redemptions")]
    public async Task<IActionResult> Redemptions(Guid id)
    {
   var promo = await _promoCodeService.GetPromoCodeDetailAsync(id);
        if (promo is null) return NotFound();

        var redemptions = await _promoCodeService.GetRedemptionHistoryAsync(id);
   ViewBag.PromoCode = promo;
    return View(redemptions);
    }
}
