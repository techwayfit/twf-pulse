# Database Schema Management Strategy

## Overview

TechWayFit Pulse uses **different schema management approaches** for different database providers:

| Provider | Approach | Reason |
|----------|----------|--------|
| **SQLite** | EF Core Migrations | Simple, single-file, dev-friendly |
| **SQL Server** | Manual SQL Scripts | Enterprise control, DBA approval, versioning |
| **InMemory** | EnsureCreated() | Testing only, ephemeral |

## SQL Server: Manual Script Approach

### Why Manual Scripts?

? **Enterprise Compliance**: DBAs can review and approve all changes  
? **Version Control**: Scripts are explicit artifacts in source control  
? **Deployment Control**: Deploy scripts independently of application  
? **Rollback Strategy**: Create explicit rollback scripts  
? **Audit Trail**: Clear history of what changed and when  
? **Multi-Environment**: Same scripts work across dev/staging/prod  
? **Team Coordination**: Avoid merge conflicts in migration files  

### ? Why NOT EF Migrations for SQL Server?

- ? Auto-generated code lacks enterprise controls
- ? Difficult to review diffs in generated migration files
- ? Hard to customize for enterprise requirements (e.g., tablespace, partitioning)
- ? Merge conflicts when multiple developers create migrations
- ? DBAs prefer reviewing SQL scripts, not C# code
- ? Production deployments require manual review anyway

## SQLite: EF Core Migrations

### Why Migrations for SQLite?

? **Development Speed**: Quick schema changes during development  
? **Automatic**: `dotnet ef migrations add` and `dotnet ef database update`  
? **Single File**: SQLite is a file, easy to delete and recreate  
? **Testing**: Integration tests can easily reset schema  
? **Lightweight**: No DBA approval needed for local dev databases  

### How to Use

```bash
# Add a migration (from Web project directory)
cd src/TechWayFit.Pulse.Web
dotnet ef migrations add AddNewFeature

# Apply migration
dotnet ef database update

# Rollback
dotnet ef database update PreviousMigration

# Remove last migration
dotnet ef migrations remove
```

## SQL Server: Script Management

### Directory Structure

```
src/TechWayFit.Pulse.Infrastructure/Scripts/
??? V1.0/  # Version 1.0 baseline
?   ??? README.md
?   ??? 00_MasterSetup.sql     # All-in-one setup
?   ??? 01_CreateSchema.sql         # Schema creation
?   ??? 02_CreateTables.sql         # Table creation
?   ??? 03_CreateIndexes.sql        # Index creation
??? V1.1/    # Version 1.1 changes
?   ??? README.md
? ??? 01_AddSessionTemplates.sql  # New table
?   ??? 99_Rollback.sql        # Rollback script
??? V2.0/ # Version 2.0 changes
    ??? README.md
    ??? 01_AlterSessions.sql        # Schema changes
    ??? 02_MigrateData.sql   # Data migration
    ??? 99_Rollback.sql   # Rollback script
```

### Naming Conventions

#### Forward Migrations
```
<Version>/<SequenceNumber>_<Description>.sql

Examples:
V1.0/01_CreateSchema.sql
V1.0/02_CreateTables.sql
V1.1/01_AddSessionTemplates.sql
V1.1/02_AddEmailTemplates.sql
V2.0/01_AlterSessionsAddScheduling.sql
```

#### Rollback Scripts
```
<Version>/99_Rollback.sql
<Version>/99_Rollback_<Feature>.sql

Examples:
V1.1/99_Rollback.sql      # Rollback entire V1.1
V1.1/99_Rollback_EmailTemplates.sql  # Rollback specific feature
```

### Script Template

#### Forward Migration Template

```sql
/*
 * TechWayFit Pulse - Database Migration
 * Version: V1.1
 * Script: 01_AddSessionTemplates.sql
 * Description: Add SessionTemplates table for template management
 * Author: [Your Name]
 * Date: 2026-01-23
 * JIRA: PULSE-123
 */

-- Check if migration already applied
IF NOT EXISTS (SELECT 1 FROM pulse.__MigrationHistory WHERE MigrationId = 'V1.1_01_AddSessionTemplates')
BEGIN
    PRINT 'Applying migration: V1.1_01_AddSessionTemplates'
    
 -- Your changes here
    CREATE TABLE pulse.SessionTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
     -- ... more columns
   CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    
    -- Create indexes
    CREATE INDEX IX_SessionTemplates_Category 
        ON pulse.SessionTemplates(Category);
    
  -- Record migration
    INSERT INTO pulse.__MigrationHistory (MigrationId, ProductVersion, AppliedDate)
    VALUES ('V1.1_01_AddSessionTemplates', '1.1.0', SYSDATETIMEOFFSET());
    
    PRINT 'Migration applied successfully: V1.1_01_AddSessionTemplates'
END
ELSE
BEGIN
    PRINT 'Migration already applied: V1.1_01_AddSessionTemplates (skipping)'
END
GO
```

#### Rollback Template

```sql
/*
 * TechWayFit Pulse - Database Rollback
 * Version: V1.1
 * Script: 99_Rollback.sql
 * Description: Rollback all V1.1 changes
 * Author: [Your Name]
 * Date: 2026-01-23
 * JIRA: PULSE-123
 * 
 * WARNING: This will remove all V1.1 features. Review carefully before executing.
 */

-- Rollback in reverse order of application
PRINT 'Rolling back V1.1 migrations...'

-- Remove V1.1_01_AddSessionTemplates
IF EXISTS (SELECT 1 FROM pulse.__MigrationHistory WHERE MigrationId = 'V1.1_01_AddSessionTemplates')
BEGIN
    PRINT 'Rolling back: V1.1_01_AddSessionTemplates'
    
  -- Drop table
    DROP TABLE IF EXISTS pulse.SessionTemplates;
    
    -- Remove migration record
    DELETE FROM pulse.__MigrationHistory 
    WHERE MigrationId = 'V1.1_01_AddSessionTemplates';
    
    PRINT 'Rollback completed: V1.1_01_AddSessionTemplates'
END

PRINT 'V1.1 rollback completed'
GO
```

### Migration History Table

SQL Server tracks applied migrations in `pulse.__MigrationHistory`:

```sql
CREATE TABLE pulse.__MigrationHistory (
    MigrationId NVARCHAR(150) PRIMARY KEY,
    ProductVersion NVARCHAR(32) NOT NULL,
    AppliedDate DATETIMEOFFSET NOT NULL
);
```

### How to Apply Migrations

#### Using sqlcmd (Recommended)

```bash
# Apply single script
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/01_AddSessionTemplates.sql"

# Apply all scripts in a version
for file in Scripts/V1.1/*.sql; do
    sqlcmd -S localhost -d TechWayFitPulse -E -i "$file"
done

# With Azure SQL Database
sqlcmd -S myserver.database.windows.net -d TechWayFitPulse -U myuser -P mypass -i "Scripts/V1.1/01_AddSessionTemplates.sql"
```

#### Using Azure Data Studio

1. Open Azure Data Studio
2. Connect to SQL Server
3. Open script file
4. Review script
5. Execute (F5)

#### Using SQL Server Management Studio (SSMS)

1. Open SSMS
2. Connect to SQL Server
3. Open script file (File > Open > File)
4. Review script
5. Execute (F5 or Execute button)

### Deployment Process

#### Development Environment
```bash
# 1. Create new migration script
touch Scripts/V1.1/01_AddFeature.sql

# 2. Write SQL in script (use template above)
# 3. Test locally
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/01_AddFeature.sql"

# 4. Verify changes
sqlcmd -S localhost -d TechWayFitPulse -E -Q "SELECT * FROM pulse.NewTable"

# 5. Commit to source control
git add Scripts/V1.1/01_AddFeature.sql
git commit -m "feat: add feature X (PULSE-123)"
```

#### Staging Environment
```bash
# 1. Pull latest scripts from git
git pull origin main

# 2. Review scripts
cat Scripts/V1.1/01_AddFeature.sql

# 3. Apply to staging
sqlcmd -S staging-server -d TechWayFitPulse -U staginguser -P $STAGING_PWD -i "Scripts/V1.1/01_AddFeature.sql"

# 4. Verify migration applied
sqlcmd -S staging-server -d TechWayFitPulse -U staginguser -P $STAGING_PWD -Q "SELECT * FROM pulse.__MigrationHistory WHERE MigrationId LIKE 'V1.1%'"

# 5. Test application
dotnet test
```

#### Production Environment
```bash
# 1. Create deployment package
mkdir -p deploy/V1.1
cp Scripts/V1.1/*.sql deploy/V1.1/

# 2. DBA Review (mandatory)
# - Send scripts to DBA team
# - Review for performance, security, compliance
# - Get approval

# 3. Schedule maintenance window
# 4. Backup database
sqlcmd -S prod-server -Q "BACKUP DATABASE TechWayFitPulse TO DISK='C:\Backups\TechWayFitPulse_PreV1.1.bak'"

# 5. Apply scripts (DBA or automated pipeline)
sqlcmd -S prod-server -d TechWayFitPulse -U produser -P $PROD_PWD -i "deploy/V1.1/01_AddFeature.sql"

# 6. Verify
sqlcmd -S prod-server -d TechWayFitPulse -U produser -P $PROD_PWD -Q "SELECT * FROM pulse.__MigrationHistory WHERE MigrationId LIKE 'V1.1%'"

# 7. Deploy application
# 8. Smoke test
# 9. Monitor for 24 hours
```

### Rollback Process

```bash
# If something goes wrong after deployment:

# 1. Stop application
systemctl stop techwayfit-pulse

# 2. Run rollback script
sqlcmd -S prod-server -d TechWayFitPulse -U produser -P $PROD_PWD -i "deploy/V1.1/99_Rollback.sql"

# 3. Verify rollback
sqlcmd -S prod-server -d TechWayFitPulse -U produser -P $PROD_PWD -Q "SELECT * FROM pulse.__MigrationHistory WHERE MigrationId LIKE 'V1.1%'"

# 4. Restore previous application version
# 5. Start application
systemctl start techwayfit-pulse

# 6. Verify system operational
```

## Best Practices

### Script Development

1. **Idempotent**: Scripts should be safe to run multiple times
   ```sql
   IF NOT EXISTS (SELECT 1 FROM pulse.__MigrationHistory WHERE MigrationId = 'V1.1_01')
   BEGIN
       -- Your changes
   END
   ```

2. **Transaction Safety**: Use transactions for data changes
   ```sql
   BEGIN TRANSACTION;
   BEGIN TRY
       -- Your changes
       COMMIT TRANSACTION;
   END TRY
   BEGIN CATCH
       ROLLBACK TRANSACTION;
       THROW;
   END CATCH
   ```

3. **Comments**: Document why, not just what
   ```sql
   -- Add Index for session lookup performance
   -- JIRA: PULSE-123
   -- Tested with 1M rows: 500ms -> 5ms
   CREATE INDEX IX_Sessions_Code ON pulse.Sessions(Code);
   ```

4. **Validation**: Include verification queries
   ```sql
 -- Verify index created
   IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sessions_Code')
    THROW 50000, 'Index IX_Sessions_Code not created', 1;
   ```

### Version Management

1. **Semantic Versioning**: V{Major}.{Minor}/
2. **Breaking Changes**: Increment major version
3. **New Features**: Increment minor version
4. **Bug Fixes**: Patch version in script name

### Testing

1. **Local Testing**: Test on local SQL Server first
2. **Staging Testing**: Apply to staging before production
3. **Load Testing**: Test with production-like data volumes
4. **Rollback Testing**: Always test rollback scripts

### Documentation

1. **README per version**: Document what changed
2. **JIRA References**: Link to tickets
3. **Breaking Changes**: Highlight in bold
4. **Data Migration**: Document any data transformations

## Tools and Automation

### Azure DevOps Pipeline

```yaml
# azure-pipelines-db.yml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - src/TechWayFit.Pulse.Infrastructure/Scripts/**

stages:
- stage: ValidateScripts
    jobs:
      - job: SQLLint
  steps:
          - task: SqlAzureDacpacDeployment@1
        inputs:
 scriptType: 'InlineScript'
     inlineScript: 'SELECT 1' # Validation logic

  - stage: DeployToStaging
    dependsOn: ValidateScripts
 jobs:
  - job: ApplyMigrations
        steps:
          - task: SqlAzureDacpacDeployment@1
        inputs:
        azureSubscription: 'Staging'
   authenticationType: 'connectionString'
connectionString: $(StagingConnectionString)
  scriptPath: 'Scripts/V1.1/*.sql'

  - stage: DeployToProduction
  dependsOn: DeployToStaging
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    jobs:
      - deployment: ProductionApproval
        environment: 'Production'
        strategy:
          runOnce:
     deploy:
    steps:
 - task: SqlAzureDacpacDeployment@1
      inputs:
          scriptPath: 'Scripts/V1.1/*.sql'
```

## Summary

| Aspect | SQL Server | SQLite |
|--------|-----------|--------|
| **Management** | Manual SQL Scripts | EF Core Migrations |
| **Versioning** | Explicit version folders | Migration timestamp |
| **Review** | DBA approval required | Developer decision |
| **Deployment** | Pipeline or manual | Automatic on startup |
| **Rollback** | Explicit rollback scripts | `dotnet ef database update PreviousMigration` |
| **Testing** | Required in staging | Optional |
| **Idempotency** | Required | Handled by EF |
| **Documentation** | Required in README | Optional |

---

**Key Takeaway**: Different databases, different strategies. SQL Server = Enterprise control, SQLite = Developer speed.
