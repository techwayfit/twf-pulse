using Microsoft.AspNetCore.Mvc;

namespace TechWayFit.Pulse.Web.Controllers;

[Route("error")]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        ViewData["StatusCode"] = statusCode;
        
        switch (statusCode)
        {
            case 404:
                _logger.LogWarning("404 Not Found: {Path}", HttpContext.Request.Path);
                ViewData["ErrorTitle"] = "Page Not Found";
                ViewData["ErrorMessage"] = "The page you're looking for doesn't exist or you don't have access to it.";
                break;
            
            case 403:
                _logger.LogWarning("403 Forbidden: {Path}", HttpContext.Request.Path);
                ViewData["ErrorTitle"] = "Access Denied";
                ViewData["ErrorMessage"] = "You don't have permission to access this resource.";
                break;
            
            case 500:
                _logger.LogError("500 Internal Server Error: {Path}", HttpContext.Request.Path);
                ViewData["ErrorTitle"] = "Server Error";
                ViewData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                break;
            
            default:
                _logger.LogWarning("HTTP {StatusCode}: {Path}", statusCode, HttpContext.Request.Path);
                ViewData["ErrorTitle"] = $"Error {statusCode}";
                ViewData["ErrorMessage"] = "An error occurred while processing your request.";
                break;
        }

        return View("Error");
    }
}
