# Developer Quick Reference: Database Changes

## SQLite (Development)

### Common Commands
```bash
# Add migration
cd src/TechWayFit.Pulse.Web
dotnet ef migrations add AddNewFeature

# Apply migration
dotnet ef database update

# Rollback one migration
dotnet ef database update PreviousMigrationName

# Remove last unapplied migration
dotnet ef migrations remove

# List all migrations
dotnet ef migrations list

# Generate SQL script (don't apply)
dotnet ef migrations script

# Reset database (CAUTION: Deletes all data)
rm pulse.db
dotnet ef database update
```

### When to Use
- ? Adding/removing tables
- ? Adding/removing columns
- ? Changing column types
- ? Adding/removing indexes
- ? Renaming tables/columns

### Workflow
```bash
# 1. Make changes to entity classes
# 2. Create migration
dotnet ef migrations add MyFeature

# 3. Review generated migration in Migrations/ folder
# 4. Apply migration
dotnet ef database update

# 5. Test changes
dotnet run

# 6. Commit
git add Migrations/
git commit -m "feat: add MyFeature migration"
```

## SQL Server (Production)

### ?? NO EF MIGRATIONS - USE SQL SCRIPTS ONLY

### Directory Structure
```
src/TechWayFit.Pulse.Infrastructure/Scripts/
??? V1.1/  # Current version
    ??? README.md      # What's in this version
    ??? 01_AddFeature.sql   # Forward migration
    ??? 02_AddIndex.sql     # Another change
    ??? 99_Rollback.sql     # Rollback script
```

### Creating a New Script

```bash
# 1. Create new version folder (if needed)
mkdir -p src/TechWayFit.Pulse.Infrastructure/Scripts/V1.1

# 2. Create script
touch src/TechWayFit.Pulse.Infrastructure/Scripts/V1.1/01_AddFeature.sql
```

### Script Template (Copy-Paste)
```sql
/*
 * Version: V1.1
 * Script: 01_AddFeature.sql
 * Description: Add new feature
 * Author: Your Name
 * Date: 2026-01-23
 */

IF NOT EXISTS (SELECT 1 FROM pulse.__MigrationHistory 
     WHERE MigrationId = 'V1.1_01_AddFeature')
BEGIN
  PRINT 'Applying: V1.1_01_AddFeature'
    
    -- Your SQL here
    CREATE TABLE pulse.MyNewTable (
 Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  Name NVARCHAR(200) NOT NULL,
CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    
    -- Record migration
    INSERT INTO pulse.__MigrationHistory (MigrationId, ProductVersion, AppliedDate)
    VALUES ('V1.1_01_AddFeature', '1.1.0', SYSDATETIMEOFFSET());
    
    PRINT 'Applied: V1.1_01_AddFeature'
END
ELSE
    PRINT 'Skipped: V1.1_01_AddFeature (already applied)'
GO
```

### Rollback Template (Copy-Paste)
```sql
/*
 * Version: V1.1
 * Script: 99_Rollback.sql
 * Description: Rollback V1.1 changes
 */

-- Rollback 01_AddFeature
IF EXISTS (SELECT 1 FROM pulse.__MigrationHistory 
     WHERE MigrationId = 'V1.1_01_AddFeature')
BEGIN
    PRINT 'Rolling back: V1.1_01_AddFeature'
    
    DROP TABLE IF EXISTS pulse.MyNewTable;
    
    DELETE FROM pulse.__MigrationHistory 
    WHERE MigrationId = 'V1.1_01_AddFeature';
    
    PRINT 'Rolled back: V1.1_01_AddFeature'
END
GO
```

### Testing Locally
```bash
# Apply script
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/01_AddFeature.sql"

# Verify
sqlcmd -S localhost -d TechWayFitPulse -E -Q "SELECT * FROM pulse.__MigrationHistory"

# Test rollback
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/99_Rollback.sql"
```

### Deployment Checklist
- [ ] Script is idempotent (safe to run multiple times)
- [ ] Migration ID is unique
- [ ] Tested locally
- [ ] Rollback script created
- [ ] README.md updated
- [ ] Code review approved
- [ ] DBA review requested (for production)

### Workflow
```bash
# 1. Create script
touch Scripts/V1.1/01_MyFeature.sql

# 2. Write SQL (use template above)

# 3. Test locally
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/01_MyFeature.sql"

# 4. Create rollback
touch Scripts/V1.1/99_Rollback.sql

# 5. Test rollback
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/99_Rollback.sql"

# 6. Re-apply
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/01_MyFeature.sql"

# 7. Update README
echo "## V1.1 Changes" >> Scripts/V1.1/README.md
echo "- Added MyFeature table" >> Scripts/V1.1/README.md

# 8. Commit
git add Scripts/V1.1/
git commit -m "feat: add MyFeature (PULSE-123)"

# 9. Create PR for DBA review
```

## Common Scenarios

### Adding a New Table

**SQLite:**
```bash
# 1. Add entity class in Domain/Entities/
# 2. Add DbSet in IPulseDbContext
# 3. Add configuration in PulseDbContextBase
# 4. Create migration
dotnet ef migrations add AddMyFeature
dotnet ef database update
```

**SQL Server:**
```sql
-- Scripts/V1.1/01_AddMyFeature.sql
CREATE TABLE pulse.MyFeature (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

CREATE INDEX IX_MyFeature_Name ON pulse.MyFeature(Name);
```

### Adding a Column

**SQLite:**
```bash
# 1. Add property to entity
# 2. Create migration
dotnet ef migrations add AddEmailToUser
dotnet ef database update
```

**SQL Server:**
```sql
-- Scripts/V1.1/02_AddEmailToUser.sql
IF NOT EXISTS (SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('pulse.Users') 
     AND name = 'Email')
BEGIN
    ALTER TABLE pulse.Users
    ADD Email NVARCHAR(256) NULL;
    
    CREATE INDEX IX_Users_Email ON pulse.Users(Email);
END
```

### Renaming a Column

**SQLite:**
```bash
# SQLite doesn't support RENAME COLUMN easily
# Need to recreate table (EF handles this)
dotnet ef migrations add RenameUserNameToDisplayName
dotnet ef database update
```

**SQL Server:**
```sql
-- Scripts/V1.1/03_RenameColumn.sql
EXEC sp_rename 'pulse.Users.UserName', 'DisplayName', 'COLUMN';
```

### Adding an Index

**SQLite:**
```bash
dotnet ef migrations add AddIndexOnSessionCode
dotnet ef database update
```

**SQL Server:**
```sql
-- Scripts/V1.1/04_AddSessionCodeIndex.sql
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
      WHERE name = 'IX_Sessions_Code')
BEGIN
    CREATE UNIQUE INDEX IX_Sessions_Code 
    ON pulse.Sessions(Code);
END
```

### Data Migration

**SQLite:**
```bash
# 1. Create migration with Up/Down methods
dotnet ef migrations add MigrateOldData

# 2. Edit migration file to add data logic
public partial class MigrateOldData : Migration
{
  protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE Sessions SET Status = 1 WHERE Status IS NULL");
    }
}

# 3. Apply
dotnet ef database update
```

**SQL Server:**
```sql
-- Scripts/V1.1/05_MigrateData.sql
BEGIN TRANSACTION;

UPDATE pulse.Sessions 
SET Status = 1 
WHERE Status IS NULL;

COMMIT TRANSACTION;
```

## Troubleshooting

### "Migration already applied"
**SQLite:**
```bash
# Check applied migrations
dotnet ef migrations list

# Remove from history
# Option 1: Reset database
rm pulse.db
dotnet ef database update

# Option 2: Manually remove from __EFMigrationsHistory table
```

**SQL Server:**
```sql
-- Check applied migrations
SELECT * FROM pulse.__MigrationHistory;

-- Remove specific migration (if not yet applied)
DELETE FROM pulse.__MigrationHistory 
WHERE MigrationId = 'V1.1_01_AddFeature';
```

### "Column already exists"
**SQLite:**
```bash
# Remove the migration and recreate
dotnet ef migrations remove
dotnet ef migrations add FixedMigration
```

**SQL Server:**
```sql
-- Add IF NOT EXISTS check
IF NOT EXISTS (SELECT 1 FROM sys.columns ...)
BEGIN
    ALTER TABLE ...
END
```

### "Cannot drop table, in use"
**SQL Server:**
```bash
# Stop application first
systemctl stop techwayfit-pulse

# Then run script
sqlcmd -S localhost -d TechWayFitPulse -E -i "Scripts/V1.1/99_Rollback.sql"
```

## Quick Reference Table

| Task | SQLite Command | SQL Server Script |
|------|---------------|------------------|
| Add table | `dotnet ef migrations add AddTable` | `CREATE TABLE pulse.Table (...)` |
| Add column | `dotnet ef migrations add AddColumn` | `ALTER TABLE pulse.Table ADD Column` |
| Add index | `dotnet ef migrations add AddIndex` | `CREATE INDEX IX_... ON pulse.Table(...)` |
| Rename column | `dotnet ef migrations add RenameColumn` | `EXEC sp_rename '...', '...', 'COLUMN'` |
| Data migration | Edit migration Up/Down | `UPDATE pulse.Table SET ...` |
| Rollback | `dotnet ef database update PreviousMigration` | Run `99_Rollback.sql` |
| List migrations | `dotnet ef migrations list` | `SELECT * FROM pulse.__MigrationHistory` |

## Environment-Specific Notes

| Environment | Database | Approach | Who Applies |
|-------------|----------|----------|-------------|
| **Local Dev** | SQLite | EF Migrations | Developer |
| **CI/CD** | InMemory | Auto-created | Pipeline |
| **Staging** | SQL Server | Manual Scripts | Pipeline/DBA |
| **Production** | SQL Server | Manual Scripts | DBA Only |

## Remember

? **SQLite = Fast iteration** - Use EF migrations  
? **SQL Server = Controlled deployment** - Use manual scripts  
? **Never** run `dotnet ef` commands against SQL Server production  
? **Never** apply SQL scripts to SQLite (use migrations instead)  

---

**Need Help?**
- SQLite issues: Check `docs/database-schema-management-strategy.md`
- SQL Server scripts: See `src/TechWayFit.Pulse.Infrastructure/Scripts/V1.0/README.md`
- Architecture questions: Ask in #dev-database Slack channel
