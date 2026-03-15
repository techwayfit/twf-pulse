# SignalR Database Backplane - MariaDB/MySQL Scripts

Version: **1.0.0**  
Date: **2026-03-15**  
Database: **MariaDB 10.3+ / MySQL 5.7+**

---

## Overview

These SQL scripts enable the **SignalR Database Backplane** feature, which allows TechWayFit Pulse to operate in **web farm (multi-server) environments** without requiring Redis or Azure SignalR Service.

## How It Works

1. **Message Storage**: When a SignalR event is broadcast, it's stored in the `SignalRMessages` table
2. **Cross-Server Polling**: Each server polls the database every 500ms for messages from other servers
3. **Local Broadcast**: Messages from other servers are broadcast to local SignalR clients
4. **Auto-Cleanup**: Messages older than 5 minutes are automatically deleted

---

## Script Files

| File | Purpose | When to Use |
|------|---------|-------------|
| `001-Create-SignalRMessages-Table.sql` | Creates the backplane table | Initial deployment or migration |
| `002-Monitoring-Queries.sql` | Health and performance monitoring | Regular monitoring, troubleshooting |
| `003-Maintenance-Scripts.sql` | Cleanup and optimization | Periodic maintenance, issue resolution |
| `004-Rollback.sql` | Removes backplane (for single-server) | Downgrade to single-server deployment |

---

## Installation

### Step 1: Run Migration Script

```bash
# Connect to your MariaDB database
mysql -u your_user -p your_database

# Run the migration
mysql> source 001-Create-SignalRMessages-Table.sql
```

**Expected Output:**
```
Query OK, 0 rows affected (0.05 sec)
```

### Step 2: Verify Installation

```sql
-- Check table exists
SELECT TABLE_NAME, ENGINE, TABLE_ROWS, TABLE_COMMENT
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'SignalRMessages';

-- Check indexes
SHOW INDEX FROM SignalRMessages;
```

### Step 3: Enable in Application

Add to `appsettings.json` or `appsettings.Production.json`:

```json
{
  "SignalR": {
"UseDatabaseBackplane": true
  }
}
```

### Step 4: Deploy to Web Farm

- Deploy application to 2+ servers
- Enable **ARR Affinity** (sticky sessions) in Azure App Service
- Monitor using queries from `002-Monitoring-Queries.sql`

---

## Monitoring

### Quick Health Check

Run this query to see overall backplane health:

```sql
-- From 002-Monitoring-Queries.sql - Query #11
SELECT 
    (SELECT COUNT(*) FROM SignalRMessages WHERE IsProcessed = 0) as PendingMessages,
 (SELECT COUNT(*) FROM SignalRMessages WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 1 MINUTE)) as LastMinute,
    (SELECT COUNT(DISTINCT ServerId) FROM SignalRMessages WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 5 MINUTE)) as ActiveServers;
```

**Healthy Values:**
- `PendingMessages`: 0-50 (low is good)
- `LastMinute`: Varies by activity (10-100 typical)
- `ActiveServers`: Should match number of web servers

### Common Monitoring Queries

```sql
-- 1. Check pending message queue
SELECT COUNT(*) as PendingMessages FROM SignalRMessages WHERE IsProcessed = 0;

-- 2. View active servers
SELECT DISTINCT ServerId, MAX(CreatedAt) as LastActivity
FROM SignalRMessages
WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 5 MINUTE)
GROUP BY ServerId;

-- 3. Check table size
SELECT ROUND(((data_length + index_length) / 1024 / 1024), 2) AS SizeMB
FROM information_schema.TABLES 
WHERE table_schema = DATABASE() AND table_name = 'SignalRMessages';
```

---

## Maintenance

### Manual Cleanup (If Needed)

```sql
-- From 003-Maintenance-Scripts.sql
DELETE FROM SignalRMessages
WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 5 MINUTE);
```

### Optimize Table (Monthly)

```sql
-- From 003-Maintenance-Scripts.sql
OPTIMIZE TABLE SignalRMessages;
ANALYZE TABLE SignalRMessages;
```

### Scheduled Cleanup (Optional)

To enable automatic database cleanup (alternative to application cleanup):

```sql
-- From 003-Maintenance-Scripts.sql - Script #14
-- Requires: SET GLOBAL event_scheduler = ON;
CREATE EVENT evt_cleanup_signalr_messages
ON SCHEDULE EVERY 5 MINUTE
DO DELETE FROM SignalRMessages WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 5 MINUTE);
```

---

## Troubleshooting

### Issue: Messages Not Syncing Between Servers

**Symptoms:**
- Participant on Server B doesn't receive updates from Server A
- `PendingMessages` count is high

**Solution:**
```sql
-- 1. Check for stuck messages
SELECT Id, ServerId, CreatedAt, TIMESTAMPDIFF(SECOND, CreatedAt, NOW()) as AgeSeconds
FROM SignalRMessages
WHERE IsProcessed = 0 AND CreatedAt < DATE_SUB(NOW(), INTERVAL 30 SECOND);

-- 2. Mark stuck messages as processed
UPDATE SignalRMessages
SET IsProcessed = 1, ProcessedAt = NOW()
WHERE IsProcessed = 0 AND CreatedAt < DATE_SUB(NOW(), INTERVAL 1 MINUTE);
```

### Issue: Table Growing Too Large

**Symptoms:**
- Table size > 100 MB
- High row count (> 10,000 rows)

**Solution:**
```sql
-- 1. Check table size
SELECT ROUND(((data_length + index_length) / 1024 / 1024), 2) AS SizeMB,
  table_rows as RowCount
FROM information_schema.TABLES 
WHERE table_schema = DATABASE() AND table_name = 'SignalRMessages';

-- 2. Aggressive cleanup (messages older than 2 minutes)
DELETE FROM SignalRMessages WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 2 MINUTE);

-- 3. Optimize table
OPTIMIZE TABLE SignalRMessages;
```

### Issue: High Database Load

**Symptoms:**
- Database CPU usage high
- Slow query logs show frequent SignalR queries

**Solution:**

1. **Increase poll interval** in `DatabaseBackplaneService.cs`:
   ```csharp
   private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(1000); // Change from 500ms to 1000ms
   ```

2. **Reduce message retention**:
   ```csharp
   private readonly TimeSpan _messageRetention = TimeSpan.FromMinutes(3); // Change from 5 to 3 minutes
   ```

3. **Verify indexes are being used**:
   ```sql
   EXPLAIN SELECT * FROM SignalRMessages 
   WHERE IsProcessed = 0 AND ServerId != 'TEST' 
   ORDER BY CreatedAt LIMIT 100;
   ```

---

## Performance Metrics

### Expected Load (3 Servers, 100 Active Sessions)

| Metric | Value |
|--------|-------|
| **SELECT queries/sec** | ~6 (2 per server) |
| **INSERT/UPDATE queries/sec** | ~5-15 (during active sessions) |
| **DELETE queries/sec** | ~0.05 (cleanup every 30 sec) |
| **Table size** | < 10 MB (with 5-min retention) |
| **Row count** | < 1,000 rows |
| **Cross-server latency** | ~500ms average |

### Capacity Limits

- **Recommended**: 2-10 servers
- **Maximum**: ~20 servers (consider Redis beyond this)
- **Sessions**: Up to 500 concurrent sessions
- **Database**: MariaDB 10.3+ or MySQL 5.7+

---

## Rollback / Uninstall

To remove the database backplane and return to single-server mode:

### Step 1: Disable in Application

```json
{
  "SignalR": {
    "UseDatabaseBackplane": false
  }
}
```

### Step 2: Run Rollback Script

```bash
mysql> source 004-Rollback.sql
```

### Step 3: Verify Removal

```sql
SELECT TABLE_NAME 
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SignalRMessages';
-- Should return 0 rows
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-03-15 | Initial release - SignalR database backplane |

---

## Support

For issues or questions:
1. Check `002-Monitoring-Queries.sql` for diagnostic queries
2. Review application logs for backplane-related errors
3. Run health check query to verify backplane status
4. See main documentation: `../../../../docs/signalr-database-backplane.md`

---

## Related Documentation

- [SignalR Database Backplane Documentation](../../../../docs/signalr-database-backplane.md)
- [Implementation Summary](../../../../IMPLEMENTATION-COMPLETE-SignalR-Database-Backplane.md)
- [SQL Server Scripts](../../SQLServer/v1.0/README.md)
- [SQLite Scripts](../../SQLite/v1.0/README.md)
