using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Application.Services;

/// <summary>
/// SMTP-based email service using MailKit for production use.
/// Configured via appsettings.json SMTP section.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
     IConfiguration configuration,
     ILogger<SmtpEmailService> logger)
{
     _configuration = configuration;
      _logger = logger;
    }

    public async Task SendLoginOtpAsync(
        string toEmail,
        string otpCode,
      string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "Hi" : $"Hi {displayName}";
        var subject = "Your TechWayFit Pulse Login Code";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
      body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; }}
   .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
   .header {{ background: linear-gradient(135deg, #2D7FF9, #2BC48A); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
      .content {{ background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; }}
 .otp-code {{ background: #f5f5f5; border: 2px solid #2D7FF9; border-radius: 8px; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #2D7FF9; margin: 20px 0; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 10px 10px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin: 20px 0; }}
        a {{ color: #2D7FF9; text-decoration: none; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
     <h1 style=""margin: 0; font-size: 24px;"">?? Your Login Code</h1>
        </div>
        <div class=""content"">
       <p>{greeting},</p>
     <p>You requested to sign in to TechWayFit Pulse. Use the code below to complete your login:</p>
            
            <div class=""otp-code"">{otpCode}</div>
   
            <p><strong>This code will expire in 10 minutes.</strong></p>
         
    <div class=""warning"">
      <strong>?? Security Notice:</strong> If you didn't request this code, please ignore this email. Never share this code with anyone.
      </div>
     
       <p>Need help? Contact our support team.</p>
      
   <p style=""margin-top: 30px;"">
       Best regards,<br>
    <strong>TechWayFit Pulse Team</strong>
            </p>
        </div>
        <div class=""footer"">
          <p>This is an automated email. Please do not reply to this message.</p>
            <p>&copy; {DateTime.UtcNow.Year} TechWayFit. All rights reserved.</p>
   </div>
    </div>
</body>
</html>";

   var plainTextBody = $@"{greeting},

You requested to sign in to TechWayFit Pulse. Use the code below to complete your login:

{otpCode}

This code will expire in 10 minutes.

?? Security Notice: If you didn't request this code, please ignore this email. Never share this code with anyone.

Best regards,
TechWayFit Pulse Team

---
This is an automated email. Please do not reply to this message.
© {DateTime.UtcNow.Year} TechWayFit. All rights reserved.";

        await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(
        string toEmail,
      string displayName,
     CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to TechWayFit Pulse! ??";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #2D7FF9, #2BC48A); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
        .content {{ background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; }}
        .features {{ background: #f8f9fa; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .feature-item {{ padding: 10px 0; border-bottom: 1px solid #e0e0e0; }}
        .feature-item:last-child {{ border-bottom: none; }}
        .cta-button {{ display: inline-block; background: #2D7FF9; color: white; padding: 12px 30px; border-radius: 6px; text-decoration: none; margin: 20px 0; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
     <div class=""header"">
      <h1 style=""margin: 0; font-size: 28px;"">Welcome to TechWayFit Pulse! ??</h1>
        </div>
        <div class=""content"">
      <p>Hi {displayName},</p>
   
            <p>Welcome to <strong>TechWayFit Pulse</strong>! Your facilitator account has been created successfully.</p>
         
   <p>You can now create and manage interactive workshop sessions with real-time participant engagement.</p>
         
  <div class=""features"">
      <h3 style=""margin-top: 0;"">What you can do:</h3>
 <div class=""feature-item"">
  ? <strong>Create Sessions</strong> - Set up workshops in seconds with customizable join forms
      </div>
      <div class=""feature-item"">
             ?? <strong>Real-time Activities</strong> - Run polls, word clouds, quadrant matrices, and 5-Whys
         </div>
                <div class=""feature-item"">
     ?? <strong>Live Dashboards</strong> - View insights and filter by participant attributes
      </div>
    <div class=""feature-item"">
        ?? <strong>Secure & Private</strong> - Passwordless authentication and encrypted sessions
 </div>
   </div>
          
            <p style=""text-align: center;"">
    <a href=""https://pulse.techway.fit/facilitator/dashboard"" class=""cta-button"">Go to Dashboard</a>
  </p>
     
            <p>If you have any questions or need help getting started, don't hesitate to reach out to our support team.</p>
       
            <p style=""margin-top: 30px;"">
        Happy facilitating! ??<br>
  <strong>TechWayFit Pulse Team</strong>
      </p>
   </div>
  <div class=""footer"">
     <p>This is an automated email. Please do not reply to this message.</p>
            <p>&copy; {DateTime.UtcNow.Year} TechWayFit. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        var plainTextBody = $@"Hi {displayName},

Welcome to TechWayFit Pulse! Your facilitator account has been created successfully.

You can now create and manage interactive workshop sessions with real-time participant engagement.

What you can do:
? Create Sessions - Set up workshops in seconds with customizable join forms
?? Real-time Activities - Run polls, word clouds, quadrant matrices, and 5-Whys
?? Live Dashboards - View insights and filter by participant attributes
?? Secure & Private - Passwordless authentication and encrypted sessions

Get started: https://pulse.techway.fit/facilitator/dashboard

If you have any questions or need help getting started, don't hesitate to reach out to our support team.

Happy facilitating! ??
TechWayFit Pulse Team

---
This is an automated email. Please do not reply to this message.
© {DateTime.UtcNow.Year} TechWayFit. All rights reserved.";

   await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody, cancellationToken);
    }

    private async Task SendEmailAsync(
        string toEmail,
     string subject,
     string htmlBody,
        string plainTextBody,
        CancellationToken cancellationToken)
{
        var smtpConfig = _configuration.GetSection("Smtp");
        
     var host = smtpConfig["Host"];
var port = int.TryParse(smtpConfig["Port"], out var p) ? p : 587;
     var username = smtpConfig["Username"];
        var password = smtpConfig["Password"];
  var fromEmail = smtpConfig["FromEmail"] ?? username;
        var fromName = smtpConfig["FromName"] ?? "TechWayFit Pulse";
    var enableSsl = bool.TryParse(smtpConfig["EnableSsl"], out var ssl) && ssl;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
    _logger.LogError("SMTP configuration is incomplete. Please check appsettings.json");
            throw new InvalidOperationException("SMTP configuration is incomplete. Check Host, Username, and Password settings.");
  }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
   {
        HtmlBody = htmlBody,
            TextBody = plainTextBody
        };

        message.Body = bodyBuilder.ToMessageBody();

    try
   {
            using var client = new SmtpClient();
  
   // Connect to SMTP server
            await client.ConnectAsync(
                host,
      port,
 enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
        cancellationToken);

            // Authenticate
   await client.AuthenticateAsync(username, password, cancellationToken);

       // Send email
            await client.SendAsync(message, cancellationToken);

 // Disconnect
    await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email} - Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
     {
      _logger.LogError(ex, "Failed to send email to {Email} - Subject: {Subject}", toEmail, subject);
    throw;
        }
}
}
