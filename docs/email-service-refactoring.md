# Email Service Refactoring Summary

## Changes Made

### 1. Created IFileService Interface
**File:** [Abstractions/Services/IFileService.cs](src/TechWayFit.Pulse.Application/Abstractions/Services/IFileService.cs)

Interface for reading files from the file system with caching support to minimize disk I/O operations.

**Key Methods:**
- `ReadFileAsync()` - Reads file content with automatic caching
- `InvalidateCache()` - Invalidates cache for a specific file
- `ClearCache()` - Clears all cached content

### 2. Implemented FileService with IMemoryCache
**File:** [Services/FileService.cs](src/TechWayFit.Pulse.Application/Services/FileService.cs)

Implements file reading with in-memory caching:
- **Cache Duration:** 1 hour absolute expiration
- **Sliding Expiration:** 30 minutes
- **Priority:** Normal
- **Logging:** Tracks cache hits, misses, and file operations

### 3. Refactored SmtpEmailService
**File:** [Services/SmtpEmailService.cs](src/TechWayFit.Pulse.Application/Services/SmtpEmailService.cs)

**Changes:**
- Removed all hardcoded HTML/text email templates
- Uses `IFileService` to load templates from disk
- Templates are cached automatically
- Added helper methods:
  - `LoadTemplateAsync()` - Loads template from file system
  - `ReplaceTokens()` - Replaces placeholders with actual values

### 4. Created Email Templates
**Location:** [App_Data/EmailTemplates/](src/TechWayFit.Pulse.Web/App_Data/EmailTemplates/)

**Templates Created:**
- `LoginOtp.html` - HTML email for login OTP codes
- `LoginOtp.txt` - Plain text version of login OTP
- `Welcome.html` - HTML email for welcome messages
- `Welcome.txt` - Plain text version of welcome email
- `README.md` - Documentation for templates

**Placeholder Tokens:**
- `{{GREETING}}` - Personalized greeting
- `{{OTP_CODE}}` - One-time password code
- `{{DISPLAY_NAME}}` - User's display name
- `{{DASHBOARD_URL}}` - Dashboard URL
- `{{CURRENT_YEAR}}` - Current year for copyright

### 5. Updated Dependency Injection
**File:** [Program.cs](src/TechWayFit.Pulse.Web/Program.cs#L117-L119)

**Registrations:**
```csharp
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IFileService, FileService>();
```

### 6. Added NuGet Package
**File:** [TechWayFit.Pulse.Application.csproj](src/TechWayFit.Pulse.Application/TechWayFit.Pulse.Application.csproj)

Added: `Microsoft.Extensions.Caching.Memory` version 10.0.1

### 7. Configuration Updates
**File:** [appsettings.json](src/TechWayFit.Pulse.Web/appsettings.json)

**New Settings:**
```json
{
  "EmailTemplates": {
    "BasePath": null  // Defaults to App_Data/EmailTemplates if null
  },
  "App": {
    "DashboardUrl": "https://pulse.techway.fit/facilitator/dashboard"
  }
}
```

## Benefits

### Performance Improvements
- ✅ **Reduced Disk I/O:** Templates cached in memory after first read
- ✅ **Fast Email Generation:** No file reads on subsequent emails
- ✅ **Efficient Memory Usage:** Cache with sliding expiration

### Maintainability
- ✅ **No Hardcoded Content:** All email text in separate template files
- ✅ **Easy Updates:** Edit templates without recompiling code
- ✅ **Separation of Concerns:** Templates separate from business logic
- ✅ **Testability:** Easy to test with mock IFileService

### Scalability
- ✅ **Cache-First Strategy:** Handles high email volume efficiently
- ✅ **Configurable Paths:** Can store templates anywhere
- ✅ **Extensible:** Easy to add new email templates

## Usage Example

### Adding a New Email Template

1. **Create Template Files:**
   ```
   App_Data/EmailTemplates/
   ├── NewEmail.html
   └── NewEmail.txt
   ```

2. **Use Placeholders:**
   ```html
   <p>Hello {{USER_NAME}},</p>
   <p>Your code is: {{CODE}}</p>
   ```

3. **Add Method to SmtpEmailService:**
   ```csharp
   public async Task SendNewEmailAsync(string email, string userName, string code)
   {
       var htmlTemplate = await LoadTemplateAsync("NewEmail.html", ct);
       var textTemplate = await LoadTemplateAsync("NewEmail.txt", ct);
       
       var replacements = new Dictionary<string, string>
       {
           { "{{USER_NAME}}", userName },
           { "{{CODE}}", code }
       };
       
       var htmlBody = ReplaceTokens(htmlTemplate, replacements);
       var textBody = ReplaceTokens(textTemplate, replacements);
       
       await SendEmailAsync(email, "Subject", htmlBody, textBody, ct);
   }
   ```

## Testing

Run the application and send emails to verify:
1. First email triggers file read and cache population
2. Subsequent emails use cached templates (check logs)
3. Templates update properly after cache expiration

## Configuration Options

### Custom Template Path
Override default template location:
```json
{
  "EmailTemplates": {
    "BasePath": "/custom/path/to/templates"
  }
}
```

### Custom Dashboard URL
Configure dashboard URL for welcome emails:
```json
{
  "App": {
    "DashboardUrl": "https://your-domain.com/dashboard"
  }
}
```

## Files Modified

1. `/src/TechWayFit.Pulse.Application/Abstractions/Services/IFileService.cs` (new)
2. `/src/TechWayFit.Pulse.Application/Services/FileService.cs` (new)
3. `/src/TechWayFit.Pulse.Application/Services/SmtpEmailService.cs` (updated)
4. `/src/TechWayFit.Pulse.Application/TechWayFit.Pulse.Application.csproj` (updated)
5. `/src/TechWayFit.Pulse.Web/Program.cs` (updated)
6. `/src/TechWayFit.Pulse.Web/appsettings.json` (updated)
7. `/src/TechWayFit.Pulse.Web/App_Data/EmailTemplates/*.html` (new)
8. `/src/TechWayFit.Pulse.Web/App_Data/EmailTemplates/*.txt` (new)
9. `/src/TechWayFit.Pulse.Web/App_Data/EmailTemplates/README.md` (new)
