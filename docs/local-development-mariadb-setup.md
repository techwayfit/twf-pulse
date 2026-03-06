# Local Development Setup - MariaDB

## Quick Start (Docker - Recommended)

### 1. Start MariaDB

```bash
# From project root
docker-compose -f docker-compose.dev.yml up -d

# Wait for MariaDB to be ready (5-10 seconds)
docker-compose -f docker-compose.dev.yml ps
```

### 2. Run Database Migrations

```bash
# Apply V1.0 baseline schema
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/00_MasterSetup.sql

# Apply V1.1 commercialization schema
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.1/00_CommercializationSchema.sql

# (Optional) Apply BackOffice schema if using BackOffice
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < backoffice/src/TechWayFit.Pulse.BackOffice.Core/Scripts/v1.0/MariaDB/00_MasterSetup.sql
```

### 3. Configure Connection String

Create `src/TechWayFit.Pulse.Web/appsettings.local.json`:

```json
{
  "ConnectionStrings": {
    "PulseDb": "Server=127.0.0.1;Port=3306;Database=pulse_dev;Uid=root;Pwd=devpassword;"
  }
}
```

### 4. Start the Application

```bash
dotnet run --project src/TechWayFit.Pulse.Web
```

App starts at: `https://localhost:5001`

---

## Alternative: Native MariaDB Installation

### Windows

**Option 1: MSI Installer**
1. Download from [mariadb.org/download](https://mariadb.org/download/)
2. Run installer (choose Developer install)
3. Set root password: `devpassword`
4. Create database:
   ```sql
   CREATE DATABASE pulse_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

**Option 2: Chocolatey**
```powershell
choco install mariadb
```

### macOS

```bash
brew install mariadb
brew services start mariadb

# Secure installation
mysql_secure_installation

# Create database
mysql -u root -p
CREATE DATABASE pulse_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### Linux (Ubuntu/Debian)

```bash
sudo apt-get update
sudo apt-get install mariadb-server

# Start service
sudo systemctl start mariadb
sudo systemctl enable mariadb

# Secure installation
sudo mysql_secure_installation

# Create database
sudo mysql
CREATE DATABASE pulse_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'pulseuser'@'localhost' IDENTIFIED BY 'devpassword';
GRANT ALL PRIVILEGES ON pulse_dev.* TO 'pulseuser'@'localhost';
FLUSH PRIVILEGES;
```

---

## Docker Commands Reference

```bash
# Start MariaDB (detached mode)
docker-compose -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.dev.yml logs -f mariadb

# Stop MariaDB
docker-compose -f docker-compose.dev.yml down

# Reset database (delete all data)
docker-compose -f docker-compose.dev.yml down -v

# Connect to MariaDB CLI
docker exec -it pulse-mariadb-dev mysql -u root -pdevpassword pulse_dev

# Backup database
docker exec pulse-mariadb-dev mysqldump -u root -pdevpassword pulse_dev > backup.sql

# Restore database
docker exec -i pulse-mariadb-dev mysql -u root -pdevpassword pulse_dev < backup.sql
```

---

## Database Management Tools

### Command Line (mysql CLI)

```bash
# Connect
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev

# List tables
SHOW TABLES;

# Check table structure
DESCRIBE Sessions;

# Count rows
SELECT COUNT(*) FROM Sessions;
```

### GUI Tools (Optional)

**Free:**
- [HeidiSQL](https://www.heidisql.com/) (Windows)
- [DBeaver](https://dbeaver.io/) (Cross-platform)
- [MySQL Workbench](https://www.mysql.com/products/workbench/) (Cross-platform)

**Paid:**
- [DataGrip](https://www.jetbrains.com/datagrip/) (JetBrains, Cross-platform)
- [TablePlus](https://tableplus.com/) (macOS, Windows)

**Connection Details:**
- Host: `127.0.0.1` or `localhost`
- Port: `3306`
- Database: `pulse_dev`
- Username: `root`
- Password: `devpassword`

---

## Troubleshooting

### "Can't connect to MySQL server"

**Check if MariaDB is running:**
```bash
# Docker
docker-compose -f docker-compose.dev.yml ps

# Native (Linux)
sudo systemctl status mariadb

# Native (macOS)
brew services list
```

**Check port 3306 is not in use:**
```bash
# Windows
netstat -ano | findstr :3306

# Linux/Mac
lsof -i :3306
```

### "Access denied for user 'root'@'localhost'"

**Reset password (Docker):**
```bash
docker-compose -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.dev.yml up -d
```

**Reset password (Native):**
```bash
sudo mysql
ALTER USER 'root'@'localhost' IDENTIFIED BY 'devpassword';
FLUSH PRIVILEGES;
```

### "Unknown database 'pulse_dev'"

```bash
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword -e "CREATE DATABASE pulse_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
```

### "Table 'pulse_dev.Sessions' doesn't exist"

Run migration scripts (see step 2 above).

### Docker volume permission issues (Linux)

```bash
# Give Docker write access to volume
sudo chown -R 999:999 /var/lib/docker/volumes/twf-pulse_pulse-mariadb-data
```

---

## Environment-Specific Configurations

### Development (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "PulseDb": "Server=127.0.0.1;Port=3306;Database=pulse_dev;Uid=root;Pwd=devpassword;"
  },
  "Email": {
    "Provider": "Console"
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "ConnectionStrings": {
 "PulseDb": "Server=prod-db.example.com;Port=3306;Database=pulse;Uid=pulseuser;Pwd=${DB_PASSWORD};SslMode=Required;"
  }
}
```

Use environment variable for password:
```bash
export ConnectionStrings__PulseDb="Server=...;Pwd=actual-password;"
```

### Local Overrides (appsettings.local.json - gitignored)

Create this file for personal settings that should never be committed:

```json
{
  "ConnectionStrings": {
    "PulseDb": "Server=127.0.0.1;Port=3306;Database=pulse_dev;Uid=root;Pwd=my-custom-password;"
  },
  "AI": {
    "Enabled": true,
 "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-your-real-api-key"
  }
  }
}
```

---

## Database Seeding

### Automated Seed Data

The following data is automatically seeded when running migration scripts:

**SubscriptionPlans (3 rows):**
- Free Plan: $0/month, 2 sessions
- Plan A: $10/month, 5 sessions, all AI features
- Plan B: $20/month, 15 sessions, all AI features

**ActivityTypeDefinitions (9 rows):**
- Free: Poll, WordCloud, Quadrant, Rating, Feedback, Q&A, Break
- Premium (Plan A+): FiveWhys, AI Summary

### Manual Seed Data (Optional)

**Create test facilitator:**
```sql
USE pulse_dev;

INSERT INTO FacilitatorUsers (Id, Email, DisplayName, CreatedAt, LastLoginAt)
VALUES (
    UUID(),
    'test@example.com',
    'Test Facilitator',
    UTC_TIMESTAMP(6),
    NULL
);
```

**Create test session:**
```sql
INSERT INTO Sessions (
    Id, Code, Title, Goal, ContextJson, SettingsJson, JoinFormSchemaJson,
  Status, CurrentActivityId, CreatedAt, UpdatedAt, ExpiresAt,
    FacilitatorUserId, GroupId, SessionStart, SessionEnd
)
VALUES (
    UUID(),
    'TEST123',
    'Development Test Session',
    NULL,
    NULL,
    '{"StrictCurrentActivityOnly":false,"RequireAuthentication":false,"LockedBackground":"https://images.unsplash.com/photo-1557804506-669a67965ba0"}',
    '[]',
    0, -- Draft
    NULL,
    UTC_TIMESTAMP(6),
    UTC_TIMESTAMP(6),
    DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 30 DAY),
    (SELECT Id FROM FacilitatorUsers WHERE Email = 'test@example.com' LIMIT 1),
    NULL,
    NULL,
    NULL
);
```

---

## Resetting the Database

### Docker (Quick Reset)

```bash
# Stop and delete volume
docker-compose -f docker-compose.dev.yml down -v

# Start fresh
docker-compose -f docker-compose.dev.yml up -d

# Re-apply migrations (see step 2 above)
```

### Native Installation

```bash
# Connect as root
mysql -u root -p

# Drop and recreate database
DROP DATABASE IF EXISTS pulse_dev;
CREATE DATABASE pulse_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
exit

# Re-apply migrations (see step 2 above)
```

---

## Performance Tips

### Index Usage

Check if queries are using indexes:

```sql
EXPLAIN SELECT * FROM Sessions WHERE Code = 'ABC123';
```

Look for `type: ref` or `type: const` (good). Avoid `type: ALL` (table scan).

### Connection Pool Configuration

For high-traffic scenarios, tune connection string:

```
Server=127.0.0.1;Port=3306;Database=pulse_dev;Uid=root;Pwd=devpassword;MinimumPoolSize=5;MaximumPoolSize=50;ConnectionIdleTimeout=30;
```

### Query Logging

Enable EF Core SQL logging in `appsettings.Development.json`:

```json
{
  "Logging": {
 "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

## Next Steps

1. ? MariaDB running locally
2. ? Database schema applied
3. ? Application configured
4. ? App starts successfully

**Now you can:**
- Create sessions at `/facilitator/create`
- Join sessions as participant at `/join`
- View BackOffice at `https://localhost:5002` (if configured)

---

## Support

**Documentation:**
- Database schema: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/README.md`
- Commercialization: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.1/README.md`
- BackOffice schema: `/backoffice/src/TechWayFit.Pulse.BackOffice.Core/Scripts/v1.0/MariaDB/README.md`

**Common Commands:**
```bash
# Check MariaDB version
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword -e "SELECT VERSION();"

# List all databases
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword -e "SHOW DATABASES;"

# Export schema only (no data)
mysqldump -h 127.0.0.1 -P 3306 -u root -pdevpassword --no-data pulse_dev > schema.sql

# Export data only (no schema)
mysqldump -h 127.0.0.1 -P 3306 -u root -pdevpassword --no-create-info pulse_dev > data.sql
```

---

**Last Updated:** March 2026  
**MariaDB Version:** 10.11  
**Status:** Production Ready ?
