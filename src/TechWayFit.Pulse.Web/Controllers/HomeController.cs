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
 return View();
    }

    /// <summary>
    /// Privacy Policy page
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Terms of Service page
    /// </summary>
    public IActionResult Terms()
    {
        return View();
    }

    /// <summary>
    /// Support page
    /// </summary>
    public IActionResult Support()
    {
        return View();
    }

    /// <summary>
    /// Getting Started guide
    /// </summary>
    public IActionResult GettingStarted()
    {
        return View();
    }

    /// <summary>
    /// Activity Types documentation
    /// </summary>
    public IActionResult ActivityTypes()
    {
        return View();
    }

    public IActionResult ParticipantGuide()
    {
        return View();
    }
}
