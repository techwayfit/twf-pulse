# Template System Refactoring Summary

## Overview
Successfully refactored the session template system from hardcoded C# code to JSON-based configuration files, making the system airtight and CI/CD friendly.

## Changes Made

### 1. Directory Structure
Created new directory structure:
```
App_Data/
└── Templates/
    ├── README.md
    ├── retro-sprint-review.json
    ├── ops-pain-points.json
    ├── product-discovery.json
    ├── incident-review.json
    └── installed/        (templates move here after processing)
```

### 2. Template JSON Files
Created 4 system template JSON files:
- **retro-sprint-review.json**: Sprint retrospective template
- **ops-pain-points.json**: Operations pain points workshop
- **product-discovery.json**: Product ideation session
- **incident-review.json**: Incident post-mortem

### 3. Code Updates

#### TemplateInitializationHostedService.cs (NEW)
- Background service that runs template initialization asynchronously
- Prevents blocking application startup
- 2-second delay ensures app is fully initialized before processing
- Scoped service provider for database access

#### SessionTemplateService.cs
- Updated `InitializeSystemTemplatesAsync()` to read from JSON files
- Added logic to process JSON templates and create/update database entries
- Implemented automatic file movement to `installed/` folder after processing
- Added new `SystemTemplateDefinition` class for JSON deserialization
- Deprecated old `GetSystemTemplateConfigs()` method (kept for backwards compatibility)

#### ISessionTemplateService.cs
- Added overload: `Task InitializeSystemTemplatesAsync(string? templatesPath, CancellationToken cancellationToken = default)`

#### TechWayFit.Pulse.Web.csproj
- Added build configuration to copy template JSON files to output directory:
  ```xml
  <None Update="App_Data\Templates\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  ```

### 4. Documentation
Created comprehensive `README.md` in Templates directory documenting:
- File format specification
- Supported activity types
- Activity-specific configurations
- Update process
- CI/CD integration guidelines

## How It Works

### Startup Process
1. Application starts **immediately** (fast startup)
2. `TemplateInitializationHostedService` runs in background (after 2-second delay)
3. Service scans `App_Data/Templates/` for `*.json` files
4. For each JSON file:
   - Deserialize to `SystemTemplateDefinition`
   - Check if template exists in database (by name)
   - Create new or update existing template
   - Move JSON file to `installed/` subfolder
5. Log processing results

**Note:** Template initialization happens asynchronously and doesn't block app startup. The app is fully operational while templates are being processed in the background.

### Benefits

**Airtight System:**
- ✅ All templates are part of the application package
- ✅ No external configuration needed
- ✅ Templates are self-contained within deployment
- ✅ Version controlled (JSON files in source control)

**Developer Friendly:**
- ✅ Easy to add new templates (just add JSON file)
- ✅ Easy to update templates (modify JSON and restart)
- ✅ Clear separation of configuration from code
- ✅ JSON is human-readable and editable

**CI/CD Ready:**
- ✅ Templates included in build output automatically
- ✅ No manual deployment steps required
- ✅ Idempotent (can re-deploy safely)
- ✅ Automatic versioning through file movement

## Adding New Templates

### Option 1: Development
1. Create new JSON file in `src/TechWayFit.Pulse.Web/App_Data/Templates/`
2. Follow the format in README.md
3. Rebuild and run application
4. Template is automatically installed

### Option 2: Production Deployment
1. Add JSON file to deployment package's `App_Data/Templates/`
2. Restart application
3. Template is automatically installed and moved to `installed/`

## Updating Existing Templates

1. Copy template from `installed/` folder back to `Templates/`
2. Modify the JSON
3. Restart application
4. Service detects existing template by name and updates it

## File Movement Behavior

- Templates in `Templates/` folder are **pending installation**
- After successful processing, they're moved to `Templates/installed/`
- This ensures templates are only processed once per deployment
- Files in `installed/` are safe (won't be reprocessed)

## Future CI/CD Automation

For automated deployments:
1. Store template JSON files in source control
2. CI pipeline builds and packages application
3. Templates are automatically included in deployment
4. On deployment to new environment:
   - Fresh templates in `Templates/` folder
   - Application processes them on first startup
   - Templates move to `installed/`
   - Subsequent restarts skip processing (no files in `Templates/`)

## Migration Notes

- Old hardcoded templates in `GetSystemTemplateConfigs()` method are deprecated
- Method kept for backwards compatibility (returns empty list)
- Can be safely removed in future version
- No database migration needed (templates created on startup)

## Testing

Build verification:
```bash
dotnet build
# ✅ Build succeeded
# ✅ Template files copied to bin/Debug/net10.0/App_Data/Templates/
```

## Rollout Plan

1. ✅ Create JSON template files
2. ✅ Update service to read from JSON
3. ✅ Update build configuration
4. ⏭️ Test with local run
5. ⏭️ Deploy to staging environment
6. ⏭️ Verify templates are created/updated
7. ⏭️ Deploy to production

## Breaking Changes

None. The system is backwards compatible:
- Existing database tempWeb/BackgroundServices/TemplateInitializationHostedService.cs` (created)
- `src/TechWayFit.Pulse.Infrastructure/Services/SessionTemplateService.cs`
- `src/TechWayFit.Pulse.Application/Abstractions/Services/ISessionTemplateService.cs`
- `src/TechWayFit.Pulse.Web/Program.cs`
- `src/TechWayFit.Pulse.Web/TechWayFit.Pulse.Web.csproj`
- `src/TechWayFit.Pulse.Web/App_Data/Templates/*.json` (1
## Files Changed

- `src/TechWayFit.Pulse.Infrastructure/Services/SessionTemplateService.cs`
- `src/TechWayFit.Pulse.Application/Abstractions/Services/ISessionTemplateService.cs`
- `src/TechWayFit.Pulse.Web/TechWayFit.Pulse.Web.csproj`
- `src/TechWayFit.Pulse.Web/App_Data/Templates/*.json` (4 files created)
- `src/TechWayFit.Pulse.Web/App_Data/Templates/README.md` (created)
