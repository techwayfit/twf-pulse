using Microsoft.AspNetCore.Mvc;

namespace TechWayFit.Pulse.Web.Controllers;

/// <summary>
/// MVC Controller for static pages (no Blazor, no WebSocket)
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Homepage - renders as pure HTML without Blazor/WebSocket overhead
    /// </summary>
    public IActionResult Index()
    {
        // Cache duration passed to view for <cache> tag helper
        ViewData["CacheDuration"] = 300; // 5 minutes in seconds
        return View();
    }

    /// <summary>
    /// Privacy Policy page
    /// </summary>
    public IActionResult Privacy()
    {
        ViewData["CacheDuration"] = 3600; // 1 hour
        return View();
    }

    /// <summary>
    /// Terms of Service page
    /// </summary>
    public IActionResult Terms()
    {
        ViewData["CacheDuration"] = 3600; // 1 hour
        return View();
    }

    /// <summary>
    /// Support page
    /// </summary>
    public IActionResult Support()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }

    /// <summary>
    /// Getting Started guide
    /// </summary>
    public IActionResult GettingStarted()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }

    /// <summary>
    /// Activity Types documentation
    /// </summary>
    public IActionResult ActivityTypes()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }

    /// <summary>
    /// Participant Guide documentation
    /// </summary>
    public IActionResult ParticipantGuide()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }

    /// <summary>
    /// Managing Sessions documentation
    /// </summary>
    public IActionResult ManagingSessions()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }

    /// <summary>
    /// AI-Powered Sessions documentation
    /// </summary>
    public IActionResult AiFeatures()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }

    /// <summary>
    /// Create from Template documentation
    /// </summary>
    public IActionResult CreateFromTemplate()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }

    /// <summary>
    /// View Components showcase
    /// </summary>
    public IActionResult Components()
    {
        ViewData["CacheDuration"] = 1800; // 30 minutes
        return View();
    }
}
