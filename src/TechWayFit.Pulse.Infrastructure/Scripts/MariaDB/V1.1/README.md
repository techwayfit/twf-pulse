# MariaDB/MySQL Schema Scripts - V1.1 Commercialization

## Overview

This directory contains MariaDB/MySQL schema scripts for **Version 1.1 - Commercialization**, which adds subscription plan management and activity type configuration to TechWayFit Pulse.

## What's New in V1.1

### New Tables (3)

1. **SubscriptionPlans** - Defines pricing tiers (Free, Plan A, Plan B)
2. **FacilitatorSubscriptions** - Tracks user subscriptions (current and historical)
3. **ActivityTypeDefinitions** - Activity metadata and premium access rules

### Seed Data

- **3 plans**: Free (2 sessions/month), Plan A ($10/month, 5 sessions), Plan B ($20/month, 15 sessions)
- **9 activity types**: Poll, WordCloud, Quadrant, FiveWhys, Rating, Feedback, Q&A, AI Summary, Break
- **Premium activities**: FiveWhys and AI Summary require Plan A or higher

## Files in This Directory

| File | Purpose | When to Run |
|------|---------|-------------|
| `00_CommercializationSchema.sql` | **All-in-one migration** (recommended) | Production deployment |
| `99_Rollback.sql` | Rollback script (drops tables and seed data) | Emergency rollback only |
| `README.md` | This file | Documentation |

## Prerequisites

- V1.0 baseline schema must be applied first
- MariaDB 10.3+ or MySQL 8.0+
- Database: `pulse` (created in V1.0)
- User with CREATE TABLE and INSERT permissions

## Quick Start

### Apply Migration

```bash
mysql -u pulseuser -p pulse < 00_CommercializationSchema.sql
```

**What it does:**
1. ? Creates 3 new tables with indexes and foreign keys
2. ? Seeds 3 subscription plans
3. ? Seeds 9 activity type definitions
4. ? Prints verification report

**Expected output:**
```
SubscriptionPlans: 3 rows
FacilitatorSubscriptions: 0 rows
ActivityTypeDefinitions: 9 rows
```

### Rollback (Emergency Only)

?? **WARNING: This deletes all subscription and activity type data!**

```bash
mysql -u pulseuser -p pulse < 99_Rollback.sql
```

**Use only if:**
- Migration failed mid-execution
- Testing in development environment
- **Never run in production without full backup**

## Verification

After running migration:

```sql
-- Check tables exist
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'pulse'
  AND TABLE_NAME IN ('SubscriptionPlans', 'FacilitatorSubscriptions', 'ActivityTypeDefinitions')
ORDER BY TABLE_NAME;

-- Check seed data
SELECT PlanCode, DisplayName, MaxSessionsPerMonth, PriceMonthly 
FROM SubscriptionPlans 
ORDER BY SortOrder;

SELECT ActivityType, DisplayName, RequiresPremium, MinPlanCode 
FROM ActivityTypeDefinitions 
WHERE IsActive = 1
ORDER BY SortOrder;
```

**Expected results:**

**SubscriptionPlans:**
```
PlanCode | DisplayName | MaxSessionsPerMonth | PriceMonthly
---------|-------------|---------------------|-------------
free     | Free        | 2        | 0.00
plan-a   | Plan A      | 5   | 10.00
plan-b   | Plan B      | 15         | 20.00
```

**ActivityTypeDefinitions:**
```
ActivityType | DisplayName  | RequiresPremium | MinPlanCode
-------------|--------------|-----------------|------------
0            | Poll      | 0          | NULL
2| Word Cloud   | 0 | NULL
5      | Quadrant     | 0               | NULL
6  | Five Whys  | 1        | plan-a
4      | Rating       | 0        | NULL
7            | Feedback     | 0    | NULL
3    | Q&A        | 0 | NULL
8         | AI Summary   | 1| plan-a
9| Break        | 0           | NULL
```

## Impact on Existing Data

### ? Safe

- **No data loss** - all existing tables unchanged
- **No schema changes** to existing tables
- **Backward compatible** - app works without subscriptions (auto-assigns Free plan)

### ?? Important Notes

1. **Existing users**: Will be auto-assigned Free plan (2 sessions/month) on first interaction
2. **Grandfathering**: Manually assign Plan B to existing active users via BackOffice after migration
3. **Activity types**: FiveWhys and AI Summary will be locked for Free plan users

### Grandfathering Existing Users (Recommended)

After migration, run this to give existing users Plan B for 90 days:

```sql
-- Grandfather all existing facilitators to Plan B for 90 days
INSERT INTO FacilitatorSubscriptions (
    Id, FacilitatorUserId, PlanId, Status, StartsAt, ExpiresAt,
    CanceledAt, SessionsUsed, SessionsResetAt,
    PaymentProvider, ExternalCustomerId, ExternalSubscriptionId,
    CreatedAt, UpdatedAt
)
SELECT 
    UUID(),
    fu.Id,
    '00000000-0000-0000-0000-000000000003', -- Plan B
  'Active',
    UTC_TIMESTAMP(6),
  DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 90 DAY),
    NULL,
    0,
    DATE_ADD(DATE_FORMAT(NOW(), '%Y-%m-01 00:00:00'), INTERVAL 1 MONTH),
    NULL,
    NULL,
    NULL,
    UTC_TIMESTAMP(6),
 UTC_TIMESTAMP(6)
FROM FacilitatorUsers fu
WHERE NOT EXISTS (
    SELECT 1 FROM FacilitatorSubscriptions fs
    WHERE fs.FacilitatorUserId = fu.Id
);

SELECT CONCAT('Grandfathered ', ROW_COUNT(), ' existing users to Plan B for 90 days') AS '';
```

## Connection Strings

### Local Development
```
Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=yourpassword;
```

### Production with SSL
```
Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;SslMode=Required;
```

### Azure Database for MySQL
```
Server=yourserver.mysql.database.azure.com;Port=3306;Database=pulse;Uid=pulseuser@yourserver;Pwd=yourpassword;SslMode=Required;
```

## Troubleshooting

### "Cannot create foreign key constraint"
**Cause:** V1.0 baseline schema not applied yet  
**Solution:** Run V1.0 script first: `src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/00_MasterSetup.sql`

### "Table already exists"
**Cause:** Script is idempotent - safe to re-run  
**Action:** No action needed, existing tables are preserved

### "Unknown database 'pulse'"
**Cause:** Database not created
**Solution:** `CREATE DATABASE pulse CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`

## Best Practices

? **Backup database** before running in production  
? **Test in staging** environment first  
? **Run during maintenance window**  
? **Verify results** after running  
? **Keep rollback script ready**  

## Related Documentation

- **Commercialization Plan**: `/docs/commercialization-plan.md`
- **V1.0 Baseline**: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/README.md`
- **SQL Server V1.1**: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.1/README.md`

---

**Version**: 1.1  
**Tables Added**: 3  
**Seed Rows**: 12  
**Status**: Production Ready ?
