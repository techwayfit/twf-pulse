# TechWayFit Pulse - Database Scripts

This directory contains SQL migration and maintenance scripts for TechWayFit Pulse infrastructure features.

---

## Directory Structure

```
Scripts/
??? MariaDB/
?   ??? v1.0/
?       ??? README.md
?       ??? 001-Create-SignalRMessages-Table.sql
?   ??? 002-Monitoring-Queries.sql
?       ??? 003-Maintenance-Scripts.sql
?       ??? 004-Rollback.sql
??? SQLServer/
?   ??? v1.0/
?       ??? (To be added)
??? SQLite/
    ??? v1.0/
      ??? (To be added)
```

---

## Features

### v1.0 - SignalR Database Backplane

**Purpose**: Enable SignalR to work across multiple servers in a web farm environment without Redis or Azure SignalR Service.

**Database Support**:
- ? **MariaDB 10.3+** (Ready)
- ? **MySQL 5.7+** (Ready)
- ?? SQL Server (Coming soon)
- ?? SQLite (Coming soon)

**Files**:
- `001-Create-SignalRMessages-Table.sql` - Initial migration
- `002-Monitoring-Queries.sql` - Health monitoring
- `003-Maintenance-Scripts.sql` - Cleanup and optimization
- `004-Rollback.sql` - Removal script

---

## Quick Start

### For MariaDB/MySQL

```bash
# 1. Navigate to scripts directory
cd src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/v1.0

# 2. Read the README
cat README.md

# 3. Run migration
mysql -u your_user -p your_database < 001-Create-SignalRMessages-Table.sql

# 4. Enable in appsettings.json
{
  "SignalR": {
    "UseDatabaseBackplane": true
  }
}
```

---

## Version History

| Version | Date | Feature | Databases |
|---------|------|---------|-----------|
| 1.0 | 2026-03-15 | SignalR Database Backplane | MariaDB, MySQL |

---

## Documentation

- **Main Docs**: `../../docs/signalr-database-backplane.md`
- **Implementation**: `../../IMPLEMENTATION-COMPLETE-SignalR-Database-Backplane.md`
- **MariaDB README**: `MariaDB/v1.0/README.md`

---

## Contributing

When adding new database scripts:

1. Create version directory: `{Provider}/v{Major}.{Minor}/`
2. Include README.md with installation instructions
3. Number scripts: `001-Description.sql`, `002-Description.sql`, etc.
4. Include monitoring, maintenance, and rollback scripts
5. Test on target database version
6. Update this master README

---

## Support

For questions or issues with database scripts:
1. Check provider-specific README (e.g., `MariaDB/v1.0/README.md`)
2. Review monitoring queries for diagnostics
3. Check application logs for migration errors
4. See main documentation in `../../docs/`
