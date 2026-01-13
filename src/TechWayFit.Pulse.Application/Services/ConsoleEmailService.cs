using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Application.Services;

/// <summary>
/// Email service implementation that logs emails to console in development.
/// Replace with real email provider (SendGrid, AWS SES, etc.) in production.
/// </summary>
public sealed class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendLoginOtpAsync(
            string toEmail,
          string otpCode,
        string? displayName = null,
            CancellationToken cancellationToken = default)
    {
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "Hi" : $"Hi {displayName}";

        var message = $@"
========================================
📧 LOGIN OTP EMAIL
========================================
To: {toEmail}
Subject: Your TechWayFit Pulse Login Code

{greeting},

Your login code is: {otpCode}

This code will expire in 10 minutes.

If you didn't request this code, please ignore this email.

Best regards,
TechWayFit Pulse Team
========================================
";

        _logger.LogInformation(message);
        Console.WriteLine(message);

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(
      string toEmail,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var message = $@"
========================================
📧 WELCOME EMAIL
========================================
To: {toEmail}
Subject: Welcome to TechWayFit Pulse!

Hi {displayName},

Welcome to TechWayFit Pulse! 🎉

Your facilitator account has been created successfully.

You can now:
• Create interactive workshop sessions
• Run live polls, word clouds, and quadrant matrices
• View real-time dashboards and insights

Get started at: https://pulse.techway.fit

Best regards,
TechWayFit Pulse Team
========================================
";

        _logger.LogInformation(message);
        Console.WriteLine(message);

        return Task.CompletedTask;
    }
}
