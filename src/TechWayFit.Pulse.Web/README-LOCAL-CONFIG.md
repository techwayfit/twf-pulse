# Local Configuration - Quick Start

## ?? Important: Credentials Moved to Local Files

Sensitive credentials (SMTP passwords, database connections) are now stored in **git-ignored local configuration files**.

## Quick Setup (First Time)

1. **Copy the template:**
   ```bash
   cd src/TechWayFit.Pulse.Web
   copy appsettings.Development.json appsettings.local.json
   ```

2. **Edit `appsettings.local.json` with your real credentials:**
   ```json
   {
     "Email": {
"Provider": "Smtp"
     },
     "Smtp": {
       "Host": "your-smtp-server.com",
       "Username": "your-username",
       "Password": "your-password",
       "FromEmail": "noreply@yourdomain.com"
     }
   }
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

## What Changed?

### Before
- ? Credentials in `appsettings.Development.json` (risky to commit)
- ? Secrets exposed in Git history

### After
- ? Credentials in `appsettings.local.json` (git-ignored)
- ? Safe defaults in `appsettings.Development.json`
- ? No risk of committing secrets

## Files

| File | Purpose | Git Status |
|------|---------|------------|
| `appsettings.json` | Base configuration | ? Tracked |
| `appsettings.Development.json` | Development defaults (safe) | ? Tracked |
| `appsettings.local.json` | **Your local secrets** | ? Ignored |
| `appsettings.Production.json` | Production config (safe) | ? Tracked |

## Configuration Priority

Files are loaded in this order (later wins):

1. `appsettings.json`
2. `appsettings.Development.json`
3. **`appsettings.local.json`** ? Your credentials here
4. Environment variables
5. Command-line args

## Verify Setup

```bash
# Check if local file is ignored
git check-ignore src/TechWayFit.Pulse.Web/appsettings.local.json
# Should output: src/TechWayFit.Pulse.Web/appsettings.local.json

# Verify local file exists
ls src/TechWayFit.Pulse.Web/appsettings.local.json
```

## Team Onboarding

When a new developer joins:

1. Clone repo
2. Copy `appsettings.Development.json` ? `appsettings.local.json`
3. Get credentials from team lead (or secrets manager)
4. Update `appsettings.local.json`
5. Run `dotnet run`

## Need Help?

See full documentation: [`docs/local-configuration-setup.md`](../docs/local-configuration-setup.md)

---

**Never commit `appsettings.local.json` - it's automatically ignored by Git!** ??
