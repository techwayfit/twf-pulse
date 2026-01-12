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
    /// Create session page - static form with JavaScript enhancement
    /// </summary>
    [HttpGet("facilitator/create")]
    public IActionResult CreateSession()
    {
   return View();
    }
}
