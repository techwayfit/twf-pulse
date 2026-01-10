using Microsoft.AspNetCore.Mvc;

namespace TechWayFit.Pulse.Web.Controllers;

[Route("ui")]
public sealed class UiController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet("facilitator")]
    public IActionResult Facilitator()
    {
        return View();
    }

    [HttpGet("participant")]
    public IActionResult Participant()
    {
        return View();
    }
}
