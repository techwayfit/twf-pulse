# SQL Server Schema Scripts - V1.0

## Overview

This directory contains **manual SQL scripts** for SQL Server schema management. EF Core migrations are **disabled** for SQL Server to maintain enterprise control over database changes.

## Why Manual Scripts?

? **DBA Review & Approval**: DBAs can review SQL before production  
? **Version Control**: Explicit scripts in git history  
? **Deployment Control**: Deploy scripts independently of application  
? **Rollback Strategy**: Clear rollback procedures for each version  
? **Audit Trail**: Track who changed what and when  
? **Multi-Environment**: Same scripts work across all environments  
? **Team Coordination**: No merge conflicts in auto-generated files  

## Files in This Directory

| File | Purpose | When to Run |
|------|---------|-------------|
| `00_MasterSetup.sql` | **All-in-one setup** (recommended) | First-time setup |
| `01_CreateSchema.sql` | Create `pulse` schema only | Manual setup |
| `02_CreateTables.sql` | Create all 10 tables | Manual setup |
| `03_CreateIndexes.sql` | Create all indexes | Manual setup |

## Quick Start

### Option 1: Master Setup (Recommended)

Run the all-in-one master script:

```bash
sqlcmd -S localhost -d TechWayFitPulse -E -i "00_MasterSetup.sql"
```

This creates:
- ? `pulse` schema
- ? 10 tables
- ? 20+ indexes
- ? Migration history table
- ? Verification report

### Option 2: Step-by-Step

Run scripts in order:

```bash
sqlcmd -S localhost -d TechWayFitPulse -E -i "01_CreateSchema.sql"
sqlcmd -S localhost -d TechWayFitPulse -E -i "02_CreateTables.sql"
sqlcmd -S localhost -d TechWayFitPulse -E -i "03_CreateIndexes.sql"
```

## What Gets Created

### Schema
- `pulse` - Main application schema

### Tables (10)
1. `pulse.Sessions` - Workshop sessions
2. `pulse.Activities` - Activities within sessions
3. `pulse.Participants` - Session participants
4. `pulse.Responses` - Participant responses
5. `pulse.ContributionCounters` - Contribution tracking
6. `pulse.FacilitatorUsers` - Facilitator accounts
7. `pulse.FacilitatorUserData` - User preferences (key-value)
8. `pulse.LoginOtps` - One-time passwords
9. `pulse.SessionGroups` - Hierarchical session organization
10. `pulse.SessionTemplates` - Reusable session templates

### Indexes (20+)
- Unique indexes on codes and emails
- Performance indexes on foreign keys
- Composite indexes for common queries
- Status and date range indexes

### Migration History
- `pulse.__MigrationHistory` - Tracks applied migrations

## Verification

After running setup scripts:

```sql
-- Check schema
SELECT * FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'pulse';

-- Check tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'pulse' 
ORDER BY TABLE_NAME;

-- Check indexes
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('pulse')
ORDER BY t.name, i.name;

-- Check migration history
SELECT * FROM pulse.__MigrationHistory;
```

Expected output:
```
Schema: pulse
Tables: 10
Indexes: 20+
Migration: V1.0_Baseline
```

## Future Changes

Future schema changes will be in versioned folders:

```
Scripts/
??? V1.0/  ? You are here
??? V1.1/  ? Future minor version
?   ??? README.md
?   ??? 01_AddFeature.sql
?   ??? 99_Rollback.sql
??? V2.0/  ? Future major version
    ??? ...
```

## Rollback

**V1.0 has no rollback** - it's the baseline schema.

To reset:
```sql
-- WARNING: This deletes ALL data
DROP SCHEMA pulse CASCADE;
```

Then re-run setup scripts.

## Connection Strings

### Local Development
```
Server=localhost;Database=TechWayFitPulse;Integrated Security=true;TrustServerCertificate=true;
```

### Azure SQL Database
```
Server=myserver.database.windows.net;Database=TechWayFitPulse;User Id=myuser;Password=mypass;Encrypt=true;
```

### SQL Server with Authentication
```
Server=localhost;Database=TechWayFitPulse;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=true;
```

## Troubleshooting

### "Database does not exist"
```sql
CREATE DATABASE TechWayFitPulse;
GO
```

### "Schema already exists"
Scripts are **idempotent** - safe to run multiple times. They check before creating.

### "Permission denied"
Ensure user has:
- `CREATE SCHEMA` permission
- `CREATE TABLE` permission
- `CREATE INDEX` permission

### "sqlcmd not found"
Install SQL Server command-line tools:
- Windows: Included with SQL Server
- Linux: `sudo apt-get install mssql-tools`
- Mac: `brew install mssql-tools`

## Best Practices

? **Always backup** before running scripts in production  
? **Test in staging** before production  
? **Review scripts** with DBA before production deployment  
? **Run during maintenance window** for production  
? **Verify results** after running scripts  
? **Keep scripts in version control** (already done)  

## Related Documentation

- **Setup Guide**: `docs/sql-server-setup.md`
- **Migration Strategy**: `docs/database-schema-management-strategy.md`
- **Quick Reference**: `docs/database-changes-quick-reference.md`
- **Architecture**: `docs/sql-server-implementation-summary.md`

## Support

Questions? Check:
1. `docs/sql-server-setup.md` - Initial setup guide
2. `docs/database-schema-management-strategy.md` - Migration strategy
3. GitHub Issues - Report problems

---

**Version**: 1.0  
**Created**: January 2026  
**Tables**: 10  
**Indexes**: 20+  
**Status**: Production Ready ?
