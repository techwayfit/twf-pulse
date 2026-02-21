# How-To Docs — TechWayFit Pulse

> Last Updated: February 2026

---

## 1. Local Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An IDE: Visual Studio 2022, VS Code, or Rider
- (Optional) SQLite browser for inspecting the dev database
- (Optional) An SMTP server or [Inbucket](https://github.com/inbucket/inbucket) for OTP email testing

### Clone and Run

```bash
git clone https://github.com/techwayfitdev/twf-pulse.git
cd twf-pulse

# Restore dependencies
dotnet restore

# Run the web project (InMemory database, Mock AI by default)
dotnet run --project src/TechWayFit.Pulse.Web
```

The app starts at `https://localhost:5001` (or the port shown in the console).

### Default Dev Configuration

By default (`appsettings.Development.json`):
- **Database**: InMemory (no setup needed, data lost on restart)
- **AI provider**: Mock (instant stub responses, no API key)
- **Email**: Console sink (OTP codes printed to terminal output)

---

## 2. Database Configuration

### Choose a Provider

| Provider | When to Use | Setup Required |
|----------|------------|---------------|
| `InMemory` | Local dev, CI/tests | None |
| `SQLite` | Staging, personal prod | None (file created automatically) |
| `SQL Server` | Enterprise/production | SQL Server instance + manual scripts |

### InMemory (default dev)

```json
// appsettings.local.json
{
  "Pulse": {
    "UseInMemory": true
  }
}
```

Data is lost when the process restarts. Use for rapid local iteration only.

### SQLite (recommended for staging/lite prod)

```json
// appsettings.local.json
{
  "Pulse": {
    "UseInMemory": false
  },
  "ConnectionStrings": {
    "PulseDb": "Data Source=App_Data/pulse.db"
  }
}
```

Run EF Core migrations to create the schema:

```bash
# Create migration (run once per schema change)
dotnet ef migrations add InitialCreate \
  --project src/TechWayFit.Pulse.Infrastructure \
  --startup-project src/TechWayFit.Pulse.Web

# Apply migrations / create database
dotnet ef database update \
  --project src/TechWayFit.Pulse.Infrastructure \
  --startup-project src/TechWayFit.Pulse.Web
```

The database file is created at `src/TechWayFit.Pulse.Web/App_Data/pulse.db`.

### SQL Server (enterprise)

```json
// appsettings.Production.json (do NOT commit credentials)
{
  "Pulse": {
    "UseInMemory": false
  },
  "ConnectionStrings": {
    "PulseDb": "Server=YOUR_SERVER;Database=TechWayFitPulse;Integrated Security=True;TrustServerCertificate=True"
  }
}
```

Apply database schema using manual scripts:
```
src/TechWayFit.Pulse.Infrastructure/Scripts/V1.0/  — initial schema
src/TechWayFit.Pulse.Infrastructure/Scripts/V1.1/  — incremental changes
```

### Configuration File Hierarchy

```
appsettings.json               # Base (committed — no secrets)
appsettings.Development.json   # Dev environment (committed)
appsettings.Production.json    # Prod environment (committed — no secrets here either)
appsettings.local.json         # Personal overrides (gitignored — put secrets here)
Environment variables          # Highest priority override
```

---

## 3. AI Configuration

AI is optional. The app runs fully without it using the `Mock` provider.

### Enable AI

Add to `appsettings.local.json`:

```json
{
  "AI": {
    "Enabled": true,
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-...",
      "Endpoint": "https://api.openai.com/v1/",
      "Model": "gpt-4o-mini",
      "TimeoutSeconds": 60
    },
    "Quota": {
      "FreeSessionsPerMonth": 5
    }
  }
}
```

### Provider Options

| `AI:Provider` | API Key Required | Notes |
|--------------|-----------------|-------|
| `OpenAI` | Yes | GPT-4o-mini default; supports Azure OpenAI endpoint |
| `Intelligent` | No | NLP + TF-IDF keyword extraction; pure C# |
| `MLNet` | No | ML.NET text featurization + classification |
| `Mock` | No | Instant stub responses; use for dev/testing |

### Use Without an API Key (Intelligent or MLNet)

```json
{
  "AI": {
    "Enabled": true,
    "Provider": "Intelligent"
  }
}
```

No API key, no cost, still generates reasonable workshop agendas.

### Azure OpenAI Endpoint

```json
{
  "AI": {
    "Enabled": true,
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "YOUR_AZURE_OPENAI_KEY",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Model": "gpt-4o-mini"
    }
  }
}
```

### Quota and BYOK

- Default quota: 5 free AI-generated sessions per facilitator per month
- Facilitators who enter their own API key in their profile bypass quota entirely (BYOK)
- Change the quota default: `AI:Quota:FreeSessionsPerMonth`

### Security

- **Never commit API keys** to source control
- Use `appsettings.local.json` (gitignored), environment variables, or `dotnet user-secrets`
- The `PiiSanitizer` utility automatically sanitizes context documents before sending to AI
- Context document summaries are capped at 500 characters per document

---

## 4. Email (OTP Login)

Facilitator authentication uses OTP (one-time password) sent by email.

### Development (Console sink — no SMTP needed)

```json
// appsettings.Development.json
{
  "Email": {
    "Provider": "Console"
  }
}
```

The OTP code is printed to the terminal — no email server needed. Check the console output after requesting a login code.

### Production (SMTP)

```json
// appsettings.local.json or environment variable
{
  "Email": {
    "Provider": "Smtp",
    "Smtp": {
      "Host": "smtp.yourprovider.com",
      "Port": 587,
      "Username": "noreply@yourorg.com",
      "Password": "YOUR_SMTP_PASSWORD",
      "FromAddress": "noreply@yourorg.com",
      "FromName": "TechWayFit Pulse"
    }
  }
}
```

---

## 5. Logging

Pulse uses **Serilog** with console and file sinks.

### Log File Location

```
src/TechWayFit.Pulse.Web/App_Data/logs/pulse-YYYYMMDD.txt
```

### Log Levels

| Environment | Default | EF Core | SignalR |
|-------------|---------|---------|---------|
| Production | Information | Warning | Warning |
| Development | Debug | Information | Debug |

### Custom Level Override

```json
// appsettings.local.json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "TechWayFit.Pulse": "Debug",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

### Injecting ILogger in Services

```csharp
public class SessionService
{
    private readonly ILogger<SessionService> _logger;

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }

    public async Task DoWorkAsync()
    {
        _logger.LogInformation("Processing session {Code}", code);
    }
}
```

---

## 6. Session Templates — Seeding

System templates are seeded from JSON files at startup by `TemplateInitializationHostedService`.

**Template files location**:
```
src/TechWayFit.Pulse.Web/App_Data/Templates/
```

Each file is a JSON document following the `SessionTemplateConfig` schema. The service reads all `.json` files in this directory on first startup and inserts them as system templates if they do not already exist.

To add a new system template:
1. Create a `.json` file in `App_Data/Templates/`
2. Follow the schema defined in `Domain/Models/SessionTemplateConfig.cs`
3. Restart the application — the template is seeded automatically

---

## 7. Running Tests

```bash
# Run all tests
dotnet test tests/TechWayFit.Pulse.Tests

# Run with detailed output
dotnet test tests/TechWayFit.Pulse.Tests --verbosity normal

# Run specific test class
dotnet test tests/TechWayFit.Pulse.Tests --filter "FullyQualifiedName~SessionServiceTests"
```

Tests use the InMemory database by default — no setup needed.

---

## 8. Build and Publish

```bash
# Build solution
dotnet build TechWayFit.Pulse.sln

# Publish for Linux (self-contained)
dotnet publish src/TechWayFit.Pulse.Web \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./publish

# Publish framework-dependent
dotnet publish src/TechWayFit.Pulse.Web \
  -c Release \
  -o ./publish
```

The `publish/` directory already contains a pre-built release with supporting config files and Docker assets.

---

## 9. Docker

A `Dockerfile` and `docker-compose.yml` are provided in `publish/`.

```bash
# Build image
docker build -t twf-pulse -f publish/Dockerfile .

# Run with docker-compose
cd publish
docker-compose up -d
```

Environment variables can be passed to the container to override any `appsettings.json` key using the standard `ASPNETCORE__` double-underscore separator:

```bash
ASPNETCORE__AI__OpenAI__ApiKey=sk-...
ASPNETCORE__ConnectionStrings__PulseDb=Data Source=/data/pulse.db
```

---

## 10. Static Files and SVG Icons

SVG icons are loaded via CSS background images — do not use `<img>` tags for icons.

```html
<!-- Correct: CSS-based icon -->
<i class="ics ics-rocket ic-lg ic-mr"></i>

<!-- Wrong: img tag -->
<img src="/images/icons/rocket.svg" />
```

See [07-ui-component-library.md](./07-ui-component-library.md) for the full icon reference and size classes.

---

## 11. Common Issues

### OTP Not Arriving

- In development, check the **terminal output** (not email) — the code is logged to console
- In production, verify SMTP credentials and port availability

### EF Core Migrations Not Found

```bash
# Ensure dotnet-ef tool is installed
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version
```

### Database Locked (SQLite)

SQLite does not support multiple writers. Ensure only one application instance is running against the same file, or switch to SQL Server for multi-instance deployments.

### AI Not Working

1. Check `AI:Enabled` is `true`
2. Verify `AI:Provider` is set correctly
3. For `OpenAI`: confirm `AI:OpenAI:ApiKey` is set in `appsettings.local.json` (not committed)
4. Check application logs in `App_Data/logs/` for AI error messages
5. Fall back to `Provider: "Intelligent"` to test without an API key
