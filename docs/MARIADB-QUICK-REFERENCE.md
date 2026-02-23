# MariaDB Quick Reference

## ? Quick Setup

```bash
# 1. Create database
mysql -u root -p -e "CREATE DATABASE pulse CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# 2. Run setup script
mysql -u root -p pulse < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/00_MasterSetup.sql

# 3. Configure (create appsettings.local.json)
cat > src/TechWayFit.Pulse.Web/appsettings.local.json << EOF
{
  "Pulse": {
  "UseInMemory": false,
 "DatabaseProvider": "MariaDB"
  },
  "ConnectionStrings": {
    "PulseDb": "Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=yourpassword;"
  }
}
EOF

# 4. Run application
dotnet run --project src/TechWayFit.Pulse.Web
```

## ?? Docker One-Liner

```bash
# Start MariaDB + Run setup script
docker run -d --name pulse-mariadb -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=pulse -p 3306:3306 mariadb:10.11 && \
sleep 10 && \
docker exec -i pulse-mariadb mysql -uroot -proot pulse < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/00_MasterSetup.sql
```

Connection string:
```
Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=root;
```

## ?? Connection Strings

### Local
```
Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=yourpassword;
```

### With SSL
```
Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;SslMode=Required;
```

### Azure MySQL
```
Server=yourserver.mysql.database.azure.com;Port=3306;Database=pulse;Uid=user@yourserver;Pwd=pass;SslMode=Required;
```

### AWS RDS
```
Server=yourinstance.region.rds.amazonaws.com;Port=3306;Database=pulse;Uid=user;Pwd=pass;SslMode=Required;
```

## ?? Configuration

### Minimal (appsettings.local.json)
```json
{
  "Pulse": {
    "DatabaseProvider": "MariaDB"
  },
  "ConnectionStrings": {
    "PulseDb": "Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=pass;"
  }
}
```

### Full Example
See: `src/TechWayFit.Pulse.Web/appsettings.MariaDB.json.example`

## ??? Database Management

### Create User
```sql
CREATE USER 'pulseuser'@'localhost' IDENTIFIED BY 'securepassword';
GRANT ALL PRIVILEGES ON pulse.* TO 'pulseuser'@'localhost';
FLUSH PRIVILEGES;
```

### Backup
```bash
mysqldump -u root -p pulse > pulse_backup.sql
```

### Restore
```bash
mysql -u root -p pulse < pulse_backup.sql
```

### Reset Database
```sql
DROP DATABASE IF EXISTS pulse;
CREATE DATABASE pulse CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- Then run setup script again
```

## ?? Quick Comparison

| Provider | Setup | Auto-Migration | Concurrency | Cost | Cross-Platform |
|----------|-------|----------------|-------------|------|----------------|
| SQLite | ? Easy | ? Yes | Low | Free | ? |
| SQL Server | ?? Manual | ? Scripts | High | $$ | ? Windows |
| **MariaDB** | ?? Manual | ? Scripts | High | Free | ? |
| InMemory | ? Auto | ? Yes | N/A | Free | ? |

## ?? Important Notes

### Version Mismatch
- Using Pomelo 9.0.0 with EF Core 10.0.3
- Works but is experimental
- Test thoroughly before production
- Update to Pomelo 10.x when available

### Manual Scripts Required
- EF migrations disabled for MariaDB (like SQL Server)
- All schema changes via SQL scripts
- Provides production control
- DBA-friendly approach

### No Schema Support
- MariaDB/MySQL doesn't use schemas like SQL Server
- Tables created directly in `pulse` database
- No `pulse.` prefix in table names

## ?? Performance Tips

### MariaDB Settings (`/etc/mysql/my.cnf`)
```ini
[mysqld]
innodb_buffer_pool_size = 1G
max_connections = 200
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci
```

### Application Settings
Connection pooling is automatic. Monitor:
- Connection count
- Query execution times
- Index usage

## ?? Common Issues

| Problem | Solution |
|---------|----------|
| "Access denied" | Check user/password, verify GRANT permissions |
| "Unknown database" | CREATE DATABASE pulse; |
| "Can't connect" | Check MariaDB is running: `sudo systemctl status mariadb` |
| "SSL error" | Use `SslMode=Preferred` or `SslMode=None` for local dev |

## ?? Documentation

- **Full Guide**: `docs/MARIADB-SUPPORT.md`
- **Setup Scripts**: `src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/README.md`
- **Configuration**: `src/TechWayFit.Pulse.Web/DATABASE-CONFIGURATION.md`
- **Implementation**: `docs/MARIADB-IMPLEMENTATION-SUMMARY.md`

## ? Status

**Build**: ? Successful  
**Files**: ? Created  
**Scripts**: ? Complete  
**Docs**: ? Complete  
**Ready**: ?? Test First

---

**Quick Start Time**: ~ 5 minutes  
**Provider**: Pomelo 9.0.0  
**Compatible**: MariaDB 10.3+ / MySQL 8.0+
