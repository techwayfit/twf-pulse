# MariaDB Schema Scripts - V1.0

## Overview

This directory contains **manual SQL scripts** for MariaDB/MySQL schema management. EF Core migrations are **disabled** for MariaDB to maintain enterprise control over database changes.

## Why Manual Scripts?

? **DBA Review & Approval**: DBAs can review SQL before production  
? **Version Control**: Explicit scripts in git history  
? **Deployment Control**: Deploy scripts independently of application  
? **Rollback Strategy**: Clear rollback procedures for each version  
? **Audit Trail**: Track who changed what and when  
? **Multi-Environment**: Same scripts work across all environments  
? **Team Coordination**: No merge conflicts in auto-generated files  

## Compatibility

- **MariaDB**: 10.3 or later (recommended 10.5+)
- **MySQL**: 8.0 or later

## Files in This Directory

| File | Purpose | When to Run |
|------|---------|-------------|
| `00_MasterSetup.sql` | **All-in-one setup** (recommended) | First-time setup |

## Quick Start

### Option 1: Master Setup (Recommended)

Run the all-in-one master script:

```bash
# For MariaDB
mysql -u root -p TechWayFitPulse < "00_MasterSetup.sql"

# Or using specific user
mysql -h localhost -u pulseuser -p TechWayFitPulse < "00_MasterSetup.sql"
```

This creates:
- ? `pulse` schema (database)
- ? 10 tables
- ? 20+ indexes
- ? Migration history table
- ? Verification report

### Option 2: MySQL Workbench

1. Open MySQL Workbench
2. Connect to your MariaDB/MySQL server
3. Open `00_MasterSetup.sql`
4. Execute the script

### Option 3: phpMyAdmin

1. Log in to phpMyAdmin
2. Select `TechWayFitPulse` database
3. Go to SQL tab
4. Paste script contents
5. Click "Go"

## What Gets Created

### Schema
- `pulse` - Main application schema (database)

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

### Data Types

MariaDB/MySQL specific types used:
- `CHAR(36)` - GUID/UUID storage
- `VARCHAR(n)` - String columns with length
- `LONGTEXT` - JSON and large text storage
- `DATETIME(6)` - Timestamps with microsecond precision
- `TINYINT(1)` - Boolean values
- `INT` - Integer values

### Storage Engine

All tables use **InnoDB** engine for:
- ACID compliance
- Foreign key support
- Row-level locking
- Crash recovery

### Character Set

All tables use **utf8mb4** with **unicode_ci** collation for:
- Full Unicode support (including emojis)
- Case-insensitive string comparisons
- International character support

## Verification

After running setup scripts:

```sql
-- Check database
SHOW DATABASES LIKE 'pulse';

-- Switch to pulse database
USE pulse;

-- Check tables
SHOW TABLES;

-- Check table structure
DESCRIBE Sessions;

-- Check indexes
SHOW INDEX FROM Sessions;

-- Verify data
SELECT COUNT(*) FROM Sessions;
```

Expected output:
```
Database: pulse
Tables: 10
Indexes: 20+
Rows: 0 (initially)
```

## Connection Strings

### Local Development

**Standard Connection**:
```
Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=yourpassword;
```

**With SSL** (recommended for production):
```
Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;SslMode=Required;
```

### Azure Database for MySQL

```
Server=yourserver.mysql.database.azure.com;Port=3306;Database=pulse;Uid=pulseuser@yourserver;Pwd=yourpassword;SslMode=Required;
```

### AWS RDS for MariaDB/MySQL

```
Server=yourinstance.region.rds.amazonaws.com;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;SslMode=Required;
```

### Docker Container

```
Server=localhost;Port=3307;Database=pulse;Uid=root;Pwd=rootpassword;
```

## Configuration in appsettings.json

```json
{
  "Pulse": {
  "UseInMemory": false,
    "DatabaseProvider": "MariaDB"
  },
  "ConnectionStrings": {
    "PulseDb": "Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=yourpassword;"
  }
}
```

**Note**: You can use either `"MariaDB"` or `"MySQL"` as the provider - both work with the same configuration.

## Create Database First

Before running the setup script, ensure the database exists:

```sql
CREATE DATABASE IF NOT EXISTS TechWayFitPulse
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
```

Or for the `pulse` schema:

```sql
CREATE DATABASE IF NOT EXISTS pulse
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
```

## User Permissions

Create a dedicated user with appropriate permissions:

```sql
-- Create user
CREATE USER 'pulseuser'@'localhost' IDENTIFIED BY 'securepassword';

-- Grant privileges on pulse database
GRANT ALL PRIVILEGES ON pulse.* TO 'pulseuser'@'localhost';

-- Apply changes
FLUSH PRIVILEGES;

-- For remote access (use specific IP instead of % for security)
CREATE USER 'pulseuser'@'%' IDENTIFIED BY 'securepassword';
GRANT ALL PRIVILEGES ON pulse.* TO 'pulseuser'@'%';
FLUSH PRIVILEGES;
```

## Troubleshooting

### "Access denied for user"
**Cause**: Authentication failure  
**Solution**: 
```sql
-- Check user exists
SELECT User, Host FROM mysql.user WHERE User = 'pulseuser';

-- Reset password
ALTER USER 'pulseuser'@'localhost' IDENTIFIED BY 'newpassword';
FLUSH PRIVILEGES;
```

### "Unknown database 'pulse'"
**Cause**: Database doesn't exist  
**Solution**: 
```sql
CREATE DATABASE pulse CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### "Can't connect to MySQL server"
**Cause**: Server not running or incorrect host/port  
**Solution**: 
```bash
# Check if MariaDB/MySQL is running
sudo systemctl status mariadb
# or
sudo systemctl status mysql

# Start if needed
sudo systemctl start mariadb
```

### "CREATE INDEX syntax error"
**Cause**: MariaDB version < 10.3 doesn't support `IF NOT EXISTS` for indexes  
**Solution**: Upgrade to MariaDB 10.3+ or manually check index existence before creation

### "Row size too large"
**Cause**: InnoDB row size limit  
**Solution**: Already handled - using `LONGTEXT` for JSON columns

## Performance Tuning

### Recommended MariaDB Settings

Add to `/etc/mysql/my.cnf` or `/etc/my.cnf`:

```ini
[mysqld]
# InnoDB settings
innodb_buffer_pool_size = 1G
innodb_log_file_size = 256M
innodb_flush_log_at_trx_commit = 2
innodb_flush_method = O_DIRECT

# Query cache (MariaDB only)
query_cache_type = 1
query_cache_size = 64M

# Connection settings
max_connections = 200
thread_cache_size = 50

# Character set
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci
```

Restart MariaDB after changes:
```bash
sudo systemctl restart mariadb
```

## Future Changes

Future schema changes will be in versioned folders:

```
Scripts/MariaDB/
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
DROP DATABASE IF EXISTS pulse;
```

Then re-run setup scripts.

## Best Practices

? **Always backup** before running scripts in production  
? **Test in staging** before production  
? **Review scripts** with DBA before production deployment  
? **Run during maintenance window** for production  
? **Verify results** after running scripts  
? **Keep scripts in version control** (already done)  
? **Use SSL** for production connections  
? **Create dedicated user** - don't use root in production  

## Docker Quick Start

Run MariaDB in Docker for local development:

```bash
# Pull and run MariaDB
docker run -d \
  --name pulse-mariadb \
  -e MYSQL_ROOT_PASSWORD=rootpassword \
  -e MYSQL_DATABASE=pulse \
  -e MYSQL_USER=pulseuser \
  -e MYSQL_PASSWORD=pulsepassword \
  -p 3306:3306 \
  mariadb:10.11

# Run setup script
docker exec -i pulse-mariadb mysql -uroot -prootpassword pulse < 00_MasterSetup.sql
```

Connection string for Docker:
```
Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=pulsepassword;
```

## Related Documentation

- **Setup Guide**: `docs/mariadb-setup.md`
- **Migration Strategy**: `docs/database-schema-management-strategy.md`
- **Architecture**: `docs/mariadb-implementation-summary.md`

## Support

Questions? Check:
1. MariaDB Documentation: https://mariadb.com/kb/en/
2. MySQL Documentation: https://dev.mysql.com/doc/
3. GitHub Issues - Report problems

---

**Version**: 1.0  
**Created**: January 2026  
**Tables**: 10  
**Indexes**: 20+  
**MariaDB**: 10.3+  
**MySQL**: 8.0+  
**Status**: Production Ready ?
