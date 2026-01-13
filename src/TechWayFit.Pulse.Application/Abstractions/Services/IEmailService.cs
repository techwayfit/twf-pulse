namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Service for sending emails (OTP codes, notifications, etc.)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a login OTP email to the specified address.
    /// </summary>
    Task SendLoginOtpAsync(string toEmail, string otpCode, string? displayName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a welcome email to a new facilitator.
    /// </summary>
  Task SendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken cancellationToken = default);
}
