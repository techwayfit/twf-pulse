using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Templates;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Controllers;

[Authorize(Policy = PolicyNames.OperatorOrAbove)]
public sealed class TemplatesController : Controller
{
    private readonly IBackOfficeTemplateService _templateService;

    public TemplatesController(IBackOfficeTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? name, TemplateCategory? category, bool? isSystem, int page = 1)
    {
        var query = new TemplateSearchQuery(name, category, isSystem, page);
        var result = await _templateService.SearchAsync(query);
        ViewBag.Query = query;
        ViewBag.Categories = Enum.GetValues<TemplateCategory>();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var detail = await _templateService.GetDetailAsync(id);
        if (detail is null) return NotFound();
        return View(detail);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = Enum.GetValues<TemplateCategory>();
        return View(new SaveTemplateRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaveTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = Enum.GetValues<TemplateCategory>();
            return View(request);
        }

        if (!IsValidJson(request.ConfigJson))
        {
            ModelState.AddModelError(nameof(request.ConfigJson), "Config must be valid JSON.");
            ViewBag.Categories = Enum.GetValues<TemplateCategory>();
            return View(request);
        }

        var (operatorId, role, ip) = OperatorContext();
        var id = await _templateService.CreateAsync(request, operatorId, role, ip);
        TempData["Success"] = $"Template '{request.Name}' created.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var detail = await _templateService.GetDetailAsync(id);
        if (detail is null) return NotFound();

        ViewBag.Categories = Enum.GetValues<TemplateCategory>();
        ViewBag.TemplateId = id;

        return View(new SaveTemplateRequest
        {
            Name = detail.Name,
            Description = detail.Description,
            Category = detail.Category,
            IconEmoji = detail.IconEmoji,
            ConfigJson = detail.ConfigJson
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SaveTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = Enum.GetValues<TemplateCategory>();
            ViewBag.TemplateId = id;
            return View(request);
        }

        if (!IsValidJson(request.ConfigJson))
        {
            ModelState.AddModelError(nameof(request.ConfigJson), "Config must be valid JSON.");
            ViewBag.Categories = Enum.GetValues<TemplateCategory>();
            ViewBag.TemplateId = id;
            return View(request);
        }

        var (operatorId, role, ip) = OperatorContext();
        await _templateService.UpdateAsync(id, request, operatorId, role, ip);
        TempData["Success"] = $"Template '{request.Name}' updated.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (operatorId, role, ip) = OperatorContext();
        await _templateService.DeleteAsync(id, operatorId, role, ip);
        TempData["Success"] = "Template deleted.";
        return RedirectToAction(nameof(Index));
    }

    private (string OperatorId, string Role, string Ip) OperatorContext() =>
        (User.Identity!.Name!,
         User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Operator",
         HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
