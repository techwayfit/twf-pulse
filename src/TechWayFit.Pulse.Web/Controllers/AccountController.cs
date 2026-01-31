using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly Application.Abstractions.Services.IAuthenticationService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
    Application.Abstractions.Services.IAuthenticationService authService,
   ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string? displayName, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Email is required.";
            return View();
        }

        var result = await _authService.SendLoginOtpAsync(email, displayName, cancellationToken);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View();
        }

        TempData["Email"] = email;
        TempData["ReturnUrl"] = returnUrl;
        TempData["DisplayName"] = displayName;
        return RedirectToAction(nameof(VerifyOtp));
    }

    [HttpGet("verify-otp")]
    public IActionResult VerifyOtp()
    {
        var email = TempData["Email"] as string;
        var returnUrl = TempData["ReturnUrl"] as string;

        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Login));
        }

        ViewData["Email"] = email;
        ViewData["ReturnUrl"] = returnUrl;
        TempData.Keep("Email");
        TempData.Keep("ReturnUrl");

        return View();
    }

    [HttpPost("verify-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(string email, string otpCode, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
        {
            TempData["Error"] = "Email and OTP code are required.";
            return RedirectToAction(nameof(VerifyOtp));
        }

        var displayName = TempData["DisplayName"] as string;
        var result = await _authService.VerifyOtpAsync(email, otpCode, displayName, cancellationToken);

        if (!result.Success || result.User == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "Invalid OTP code.";
            TempData["Email"] = email;
            TempData["ReturnUrl"] = returnUrl;
            return RedirectToAction(nameof(VerifyOtp));
        }

        // Create authentication claims
        var claims = new List<Claim>
   {
            new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
         new Claim(ClaimTypes.Email, result.User.Email),
 new Claim(ClaimTypes.Name, result.User.DisplayName),
            new Claim("FacilitatorUserId", result.User.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(
   CookieAuthenticationDefaults.AuthenticationScheme,
   new ClaimsPrincipal(claimsIdentity),
   authProperties);

        _logger.LogInformation("Facilitator {Email} logged in successfully", email);

        return RedirectToLocal(returnUrl);
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _authService.GetFacilitatorAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        return View(user);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Dashboard", "Facilitator");
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
