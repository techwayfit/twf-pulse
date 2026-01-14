# Email Template Configuration

This directory contains email templates used by the SmtpEmailService.

## Available Templates

### LoginOtp
- **LoginOtp.html** - HTML email template for login OTP codes
- **LoginOtp.txt** - Plain text version of login OTP email

**Placeholders:**
- `{{GREETING}}` - Personalized greeting (e.g., "Hi John" or "Hi")
- `{{OTP_CODE}}` - The one-time password code
- `{{CURRENT_YEAR}}` - Current year for copyright notice

### Welcome
- **Welcome.html** - HTML email template for welcome emails
- **Welcome.txt** - Plain text version of welcome email

**Placeholders:**
- `{{DISPLAY_NAME}}` - User's display name
- `{{DASHBOARD_URL}}` - URL to the dashboard
- `{{CURRENT_YEAR}}` - Current year for copyright notice

## Template Caching

Email templates are cached in memory by the `FileService` to reduce disk I/O:
- **Cache Duration:** 1 hour (absolute expiration)
- **Sliding Expiration:** 30 minutes
- Templates are loaded once and reused for subsequent emails

## Configuration

### Template Base Path
Configure the template directory in `appsettings.json`:

```json
{
  "EmailTemplates": {
    "BasePath": "/path/to/custom/templates"
  }
}
```

If not specified, defaults to: `App_Data/EmailTemplates`

### Dashboard URL
Configure the dashboard URL in `appsettings.json`:

```json
{
  "App": {
    "DashboardUrl": "https://pulse.techway.fit/facilitator/dashboard"
  }
}
```

## Adding New Templates

1. Create HTML and TXT versions of your template
2. Use `{{PLACEHOLDER}}` syntax for dynamic content
3. Save files in the `App_Data/EmailTemplates` directory
4. Update `SmtpEmailService` to load and use the new template
5. Document placeholders in this README

## Performance Notes

- Templates are read from disk only on first use
- Subsequent uses retrieve from in-memory cache
- Cache automatically expires after 1 hour
- Use `IFileService.InvalidateCache(filePath)` to force reload
