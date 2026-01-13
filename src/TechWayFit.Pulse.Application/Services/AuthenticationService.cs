using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IFacilitatorUserRepository _userRepository;
    private readonly ILoginOtpRepository _otpRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthenticationService> _logger;

    private const int OtpLength = 6;
    private const int OtpExpiryMinutes = 10;
    private const int MaxOtpAttemptsPerHour = 5;

    public AuthenticationService(
      IFacilitatorUserRepository userRepository,
        ILoginOtpRepository otpRepository,
    IEmailService emailService,
        ILogger<AuthenticationService> logger)
    {
     _userRepository = userRepository;
_otpRepository = otpRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<SendOtpResult> SendLoginOtpAsync(
        string email,
 string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
    return new SendOtpResult(false, "Email is required.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

    // Rate limiting: Check recent OTPs
     var recentOtps = await _otpRepository.GetRecentOtpsForEmailAsync(
            normalizedEmail,
            MaxOtpAttemptsPerHour,
         cancellationToken);

        var oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var recentOtpCount = recentOtps.Count(o => o.CreatedAt > oneHourAgo);

        if (recentOtpCount >= MaxOtpAttemptsPerHour)
        {
        _logger.LogWarning("Rate limit exceeded for email {Email}", normalizedEmail);
     return new SendOtpResult(false, "Too many OTP requests. Please try again later.");
        }

      // Generate OTP
  var otpCode = GenerateOtpCode();
        var now = DateTimeOffset.UtcNow;
        var otp = new LoginOtp(
            Guid.NewGuid(),
          normalizedEmail,
otpCode,
      now,
    now.AddMinutes(OtpExpiryMinutes));

     await _otpRepository.AddAsync(otp, cancellationToken);

        // Check if user exists
 var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        var userName = existingUser?.DisplayName ?? displayName ?? "there";

        // Send OTP email
      try
        {
      await _emailService.SendLoginOtpAsync(normalizedEmail, otpCode, userName, cancellationToken);
       _logger.LogInformation("Login OTP sent to {Email}", normalizedEmail);
            return new SendOtpResult(true, "OTP sent to your email.");
      }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Failed to send OTP email to {Email}", normalizedEmail);
            return new SendOtpResult(false, "Failed to send OTP. Please try again.");
        }
    }

    public async Task<VerifyOtpResult> VerifyOtpAsync(
        string email,
        string otpCode,
      CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
        {
         return new VerifyOtpResult(false, null, "Email and OTP code are required.");
  }

     var normalizedEmail = email.Trim().ToLowerInvariant();
     var normalizedOtp = otpCode.Trim();

var otp = await _otpRepository.GetValidOtpAsync(normalizedEmail, normalizedOtp, cancellationToken);

        if (otp == null)
        {
            _logger.LogWarning("Invalid OTP attempt for email {Email}", normalizedEmail);
            return new VerifyOtpResult(false, null, "Invalid or expired OTP code.");
        }

        if (!otp.IsValid(DateTimeOffset.UtcNow))
        {
            return new VerifyOtpResult(false, null, "OTP code has expired.");
        }

        // Mark OTP as used
 otp.MarkAsUsed(DateTimeOffset.UtcNow);
        await _otpRepository.UpdateAsync(otp, cancellationToken);

        // Get or create user
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null)
    {
            // Create new user
            var displayName = ExtractDisplayNameFromEmail(normalizedEmail);
         user = new FacilitatorUser(
    Guid.NewGuid(),
    normalizedEmail,
    displayName,
     DateTimeOffset.UtcNow,
       DateTimeOffset.UtcNow);

  await _userRepository.AddAsync(user, cancellationToken);

     // Send welcome email
            try
            {
      await _emailService.SendWelcomeEmailAsync(normalizedEmail, displayName, cancellationToken);
        }
   catch (Exception ex)
  {
        _logger.LogError(ex, "Failed to send welcome email to {Email}", normalizedEmail);
     // Don't fail the login if welcome email fails
  }

            _logger.LogInformation("New facilitator user created: {Email}", normalizedEmail);
        }
    else
        {
     // Update last login
       user.UpdateLastLogin(DateTimeOffset.UtcNow);
    await _userRepository.UpdateAsync(user, cancellationToken);
   }

        _logger.LogInformation("User {Email} logged in successfully", normalizedEmail);
     return new VerifyOtpResult(true, user);
    }

    public async Task<FacilitatorUser?> GetFacilitatorAsync(Guid userId, CancellationToken cancellationToken = default)
  {
        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<FacilitatorUser?> GetFacilitatorByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
    return null;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
}

    private static string GenerateOtpCode()
    {
      // Generate a 6-digit numeric OTP
 var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static string ExtractDisplayNameFromEmail(string email)
    {
   // Extract display name from email (part before @)
        var atIndex = email.IndexOf('@');
     if (atIndex > 0)
        {
var localPart = email[..atIndex];
   // Capitalize first letter
            return char.ToUpper(localPart[0]) + localPart[1..];
        }

        return "Facilitator";
    }
}
