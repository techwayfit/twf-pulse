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
/// Uses file-based templates with caching to reduce disk I/O.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IFileService _fileService;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _templateBasePath;

    public SmtpEmailService(
        IConfiguration configuration,
        IFileService fileService,
        ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _fileService = fileService;
        _logger = logger;

        // Get template base path from configuration or use default
        _templateBasePath = _configuration["EmailTemplates:BasePath"] 
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "EmailTemplates");
    }

    public async Task SendLoginOtpAsync(
        string toEmail,
        string otpCode,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "Hi" : $"Hi {displayName}";
        var subject = "Your TechWayFit Pulse Login Code";

        // Load templates from files
        var htmlTemplate = await LoadTemplateAsync("LoginOtp.html", cancellationToken);
        var textTemplate = await LoadTemplateAsync("LoginOtp.txt", cancellationToken);

        // Replace placeholders
        var replacements = new Dictionary<string, string>
        {
            { "{{GREETING}}", greeting },
            { "{{OTP_CODE}}", otpCode },
            { "{{CURRENT_YEAR}}", DateTime.UtcNow.Year.ToString() }
        };

        var htmlBody = ReplaceTokens(htmlTemplate, replacements);
        var plainTextBody = ReplaceTokens(textTemplate, replacements);

        await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to TechWayFit Pulse! 🎉";

        // Load templates from files
        var htmlTemplate = await LoadTemplateAsync("Welcome.html", cancellationToken);
        var textTemplate = await LoadTemplateAsync("Welcome.txt", cancellationToken);

        // Get dashboard URL from configuration or use default
        var dashboardUrl = _configuration["App:DashboardUrl"] 
            ?? "https://pulse.techwayfit.com/facilitator/dashboard";

        // Replace placeholders
        var replacements = new Dictionary<string, string>
        {
            { "{{DISPLAY_NAME}}", displayName },
            { "{{DASHBOARD_URL}}", dashboardUrl },
            { "{{CURRENT_YEAR}}", DateTime.UtcNow.Year.ToString() }
        };

        var htmlBody = ReplaceTokens(htmlTemplate, replacements);
        var plainTextBody = ReplaceTokens(textTemplate, replacements);

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

    /// <summary>
    /// Loads an email template from the file system.
    /// Templates are cached by the FileService to reduce disk I/O.
    /// </summary>
    private async Task<string> LoadTemplateAsync(string templateFileName, CancellationToken cancellationToken)
    {
        var templatePath = Path.Combine(_templateBasePath, templateFileName);
        
        try
        {
            return await _fileService.ReadFileAsync(templatePath, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Email template not found: {TemplatePath}", templatePath);
            throw new InvalidOperationException($"Email template not found: {templateFileName}", ex);
        }
    }

    /// <summary>
    /// Replaces tokens in the template with actual values.
    /// </summary>
    private static string ReplaceTokens(string template, Dictionary<string, string> replacements)
    {
        var result = template;
        
        foreach (var (token, value) in replacements)
        {
            result = result.Replace(token, value);
        }
        
        return result;
    }
}
