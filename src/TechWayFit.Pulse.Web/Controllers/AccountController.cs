using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Web.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly Application.Abstractions.Services.IAuthenticationService _authService;
    private readonly IFacilitatorUserDataRepository _userDataRepository;
    private readonly IAiQuotaService? _quotaService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        Application.Abstractions.Services.IAuthenticationService authService,
        IFacilitatorUserDataRepository userDataRepository,
        ILogger<AccountController> logger,
        IAiQuotaService? quotaService = null)
    {
        _authService = authService;
        _userDataRepository = userDataRepository;
        _logger = logger;
        _quotaService = quotaService;
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

        // Load user data
        var userData = await _userDataRepository.GetAllAsDictAsync(userId.Value, cancellationToken);
        ViewBag.UserData = userData;
        
        // Load quota status if service is available
        if (_quotaService != null)
        {
            var quotaStatus = await _quotaService.GetQuotaStatusAsync(userId.Value, cancellationToken);
            ViewBag.QuotaStatus = quotaStatus;
        }
        
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        ViewBag.ErrorMessage = TempData["ErrorMessage"];

        return View(user);
    }

    [HttpPost("profile/update-settings")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings(
        string? openAiApiKey,
        string? openAiBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        try
        {
            // Update OpenAI API Key if provided
            if (!string.IsNullOrWhiteSpace(openAiApiKey))
            {
                // TODO: Encrypt the API key before storing
                await _userDataRepository.SetValueAsync(
                    userId.Value,
                    FacilitatorUserDataKeys.OpenAiApiKey,
                    openAiApiKey.Trim(),
                    cancellationToken);
            }
            else
            {
                // Delete if empty
                await _userDataRepository.DeleteAsync(
                    userId.Value,
                    FacilitatorUserDataKeys.OpenAiApiKey,
                    cancellationToken);
            }

            // Update OpenAI Base URL if provided
            if (!string.IsNullOrWhiteSpace(openAiBaseUrl))
            {
                await _userDataRepository.SetValueAsync(
                    userId.Value,
                    FacilitatorUserDataKeys.OpenAiBaseUrl,
                    openAiBaseUrl.Trim(),
                    cancellationToken);
            }
            else
            {
                // Delete if empty
                await _userDataRepository.DeleteAsync(
                    userId.Value,
                    FacilitatorUserDataKeys.OpenAiBaseUrl,
                    cancellationToken);
            }

            TempData["SuccessMessage"] = "Settings updated successfully!";
            _logger.LogInformation("User {UserId} updated their profile settings", userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings for user {UserId}", userId.Value);
            TempData["ErrorMessage"] = "Failed to update settings. Please try again.";
        }

        return RedirectToAction(nameof(Profile));
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
