# Serilog Configuration

## Overview
TechWayFit Pulse now uses Serilog for structured logging with support for both console and file-based logging.

## Configuration

### Log File Location
Logs are written to: `src/TechWayFit.Pulse.Web/App_Data/logs/`

### File Naming Convention
- Files are named: `pulse-YYYYMMDD.txt`
- Example: `pulse-20260118.txt`

### Rotation Policy
- **Rolling Interval**: Daily
- **Retention**: 30 days (configurable via `retainedFileCountLimit`)

## Log Levels

### Production (`appsettings.json`)
- **Default**: Information
- **Microsoft**: Warning
- **Microsoft.AspNetCore**: Warning
- **Microsoft.EntityFrameworkCore**: Warning
- **System**: Warning

### Development (`appsettings.Development.json`)
- **Default**: Debug
- **Microsoft**: Information
- **Microsoft.AspNetCore**: Information
- **Microsoft.EntityFrameworkCore**: Information
- **Microsoft.AspNetCore.SignalR**: Debug
- **Microsoft.AspNetCore.Components.Server**: Debug
- **TechWayFit.Pulse**: Debug

## Output Templates

### Console
```
[HH:mm:ss LEVEL] SourceContext: Message
Exception (if any)
```
Example:
```
[14:32:15 INF] TechWayFit.Pulse.Application.Services.SessionService: Creating new session with code ABC123
```

### File
```
YYYY-MM-DD HH:mm:ss.fff zzz [LEVEL] SourceContext: Message
Exception (if any)
```
Example:
```
2026-01-18 14:32:15.123 +00:00 [INF] TechWayFit.Pulse.Application.Services.SessionService: Creating new session with code ABC123
```

## HTTP Request Logging

Serilog automatically logs all HTTP requests with:
- Request method and path
- Response status code
- Elapsed time in milliseconds

Example:
```
HTTP GET /facilitator/dashboard responded 200 in 45.1234 ms
```

### Log Levels for HTTP Requests
- **Error** (5xx responses or exceptions): LogEventLevel.Error
- **Warning** (4xx responses): LogEventLevel.Information
- **Information** (2xx/3xx responses): LogEventLevel.Information

## Usage in Code

### Injecting ILogger

```csharp
public class SessionService : ISessionService
{
    private readonly ILogger<SessionService> _logger;
    
    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }
    
    public async Task<Session> CreateSessionAsync(CreateSessionRequest request)
    {
        _logger.LogInformation("Creating session with title {Title}", request.Title);
   
        try
        {
 // ... implementation
    _logger.LogInformation("Session created successfully with code {Code}", session.Code);
            return session;
        }
        catch (Exception ex)
   {
            _logger.LogError(ex, "Failed to create session");
throw;
    }
 }
}
```

### Structured Logging Best Practices

? **DO**: Use structured logging with named parameters
```csharp
_logger.LogInformation("User {UserId} joined session {SessionCode}", userId, sessionCode);
```

? **DON'T**: Use string interpolation
```csharp
_logger.LogInformation($"User {userId} joined session {sessionCode}");
```

### Log Levels Guide

| Level | When to Use | Example |
|-------|-------------|---------|
| **Trace** | Very detailed diagnostic information | `_logger.LogTrace("Entering method with param {Value}", value)` |
| **Debug** | Debugging information during development | `_logger.LogDebug("Query generated: {Query}", sql)` |
| **Information** | General informational messages | `_logger.LogInformation("Session {Code} started", code)` |
| **Warning** | Warning messages for unexpected but handled situations | `_logger.LogWarning("Participant {Id} already exists in session", id)` |
| **Error** | Error messages for failures | `_logger.LogError(ex, "Failed to save response")` |
| **Critical** | Critical failures requiring immediate attention | `_logger.LogCritical(ex, "Database connection failed")` |

## Configuration

### Changing Log Levels at Runtime
Edit `appsettings.json` or `appsettings.Development.json` and restart the application.

### Changing Retention Period
```json
{
  "Serilog": {
    "WriteTo": [
 {
  "Name": "File",
        "Args": {
          "retainedFileCountLimit": 60  // Keep 60 days of logs
        }
      }
    ]
  }
}
```

### Adding Additional Sinks
To add more sinks (e.g., Seq, Application Insights), install the appropriate NuGet package and update configuration:

```bash
dotnet add package Serilog.Sinks.Seq
```

```json
{
  "Serilog": {
  "WriteTo": [
      {
"Name": "Seq",
        "Args": {
     "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

## Troubleshooting

### Logs Not Appearing
1. Check the `App_Data/logs` folder exists
2. Verify write permissions on the folder
3. Check log level configuration - it may be filtering out messages
4. Restart the application after configuration changes

### Log Files Too Large
1. Reduce retention period: `retainedFileCountLimit`
2. Increase minimum log level for noisy namespaces
3. Consider adding file size limits:
```json
{
  "Args": {
    "fileSizeLimitBytes": 10485760  // 10 MB
  }
}
```

### Performance Impact
- File logging is asynchronous by default
- Minimal performance impact for Information level and above
- If Debug/Trace logging causes issues in production, increase minimum level to Information

## Git Considerations
- The `App_Data/logs` folder structure is preserved with `.gitkeep`
- All `*.log` and `*.txt` files in logs are ignored by `.gitignore`
- Only the folder structure is committed to source control

## Package References
- **Serilog.AspNetCore** (v10.0.0): Main Serilog integration for ASP.NET Core
  - Includes: Serilog, Serilog.Extensions.Hosting, Serilog.Extensions.Logging
  - Includes: Serilog.Sinks.Console, Serilog.Sinks.File
  - Includes: Serilog.Settings.Configuration
