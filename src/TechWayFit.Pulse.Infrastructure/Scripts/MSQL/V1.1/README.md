# SQL Server Schema Scripts - V1.1 Commercialization

## Overview

This directory contains SQL Server schema scripts for **Version 1.1 - Commercialization**, which adds subscription plan management and activity type configuration to TechWayFit Pulse.

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
- SQL Server 2016 or later
- Database: `TechWayFitPulse`
- Schema: `pulse` (created in V1.0)

## Quick Start

### Apply Migration

```bash
sqlcmd -S localhost -d TechWayFitPulse -E -i "00_CommercializationSchema.sql"
```

**What it does:**
1. ? Creates 3 new tables with indexes and foreign keys
2. ? Seeds 3 subscription plans
3. ? Seeds 9 activity type definitions
4. ? Records migration in `pulse.__MigrationHistory`
5. ? Prints verification report

**Expected output:**
```
SubscriptionPlans: 3 rows
FacilitatorSubscriptions: 0 rows
ActivityTypeDefinitions: 9 rows
```

### Rollback (Emergency Only)

?? **WARNING: This deletes all subscription and activity type data!**

```bash
sqlcmd -S localhost -d TechWayFitPulse -E -i "99_Rollback.sql"
```

**Use only if:**
- Migration failed mid-execution
- Testing in development environment
- **Never run in production without full backup**

## Verification

After running migration, verify with:

```sql
-- Check tables exist
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'pulse' 
  AND TABLE_NAME IN ('SubscriptionPlans', 'FacilitatorSubscriptions', 'ActivityTypeDefinitions')
ORDER BY TABLE_NAME;

-- Check seed data
SELECT PlanCode, DisplayName, MaxSessionsPerMonth, PriceMonthly 
FROM pulse.SubscriptionPlans 
ORDER BY SortOrder;

SELECT ActivityType, DisplayName, RequiresPremium, MinPlanCode 
FROM pulse.ActivityTypeDefinitions 
WHERE IsActive = 1
ORDER BY SortOrder;

-- Check migration history
SELECT * FROM pulse.__MigrationHistory WHERE MigrationId = 'V1.1_Commercialization';
```

**Expected results:**

**SubscriptionPlans:**
```
PlanCode | DisplayName | MaxSessionsPerMonth | PriceMonthly
---------|-------------|---------------------|-------------
free     | Free        | 2       | 0.00
plan-a   | Plan A      | 5      | 10.00
plan-b   | Plan B      | 15 | 20.00
```

**ActivityTypeDefinitions:**
```
ActivityType | DisplayName  | RequiresPremium | MinPlanCode
-------------|--------------|-----------------|------------
0| Poll         | 0   | NULL
2 | Word Cloud   | 0               | NULL
5            | Quadrant     | 0      | NULL
6      | Five Whys    | 1               | plan-a
4     | Rating       | 0       | NULL
7    | Feedback   | 0          | NULL
3 | Q&A          | 0      | NULL
8            | AI Summary   | 1     | plan-a
9     | Break        | 0          | NULL
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
INSERT INTO pulse.FacilitatorSubscriptions (
    Id, FacilitatorUserId, PlanId, Status, StartsAt, ExpiresAt, 
    CanceledAt, SessionsUsed, SessionsResetAt, 
    PaymentProvider, ExternalCustomerId, ExternalSubscriptionId,
    CreatedAt, UpdatedAt
)
SELECT 
    NEWID(),
    fu.Id,
    (SELECT Id FROM pulse.SubscriptionPlans WHERE PlanCode = 'plan-b'),
    'Active',
    SYSDATETIMEOFFSET(),
    DATEADD(day, 90, SYSDATETIMEOFFSET()), -- Expires in 90 days
    NULL,
    0, -- No sessions used yet
    DATEADD(month, 1, DATETIMEOFFSETFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1, 0, 0, 0, 0, 0, 0, 0)), -- Reset on 1st of next month
    NULL, -- No payment provider (grandfathered)
    NULL,
    NULL,
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
FROM pulse.FacilitatorUsers fu
WHERE NOT EXISTS (
    SELECT 1 FROM pulse.FacilitatorSubscriptions fs 
    WHERE fs.FacilitatorUserId = fu.Id
);

PRINT 'Grandfathered ' + CAST(@@ROWCOUNT AS VARCHAR) + ' existing users to Plan B for 90 days';
```

## Connection Strings

### Local Development
```
Server=localhost;Database=TechWayFitPulse;Integrated Security=true;TrustServerCertificate=true;
```

### Azure SQL Database
```
Server=myserver.database.windows.net;Database=TechWayFitPulse;User Id=myuser;Password=mypass;Encrypt=true;
```

## Troubleshooting

### "Cannot create foreign key constraint"
**Cause:** V1.0 baseline schema not applied yet  
**Solution:** Run V1.0 scripts first: `src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.0/00_MasterSetup.sql`

### "Incorrect syntax near 'ActivityTypeDefinitions'"
**Cause:** SQL Server version < 2016  
**Solution:** Upgrade SQL Server to 2016 or later

### "PlanCode already exists"
**Cause:** Seed data already inserted (script is idempotent - safe to re-run)  
**Action:** No action needed, script will skip existing rows

## Best Practices

? **Backup database** before running migration in production  
? **Test in staging** environment first  
? **Run during maintenance window** (low traffic period)  
? **Verify results** using verification queries above  
? **Keep rollback script** ready (99_Rollback.sql)  
? **Document any manual adjustments** (e.g., grandfathering script)  

## Related Documentation

- **Commercialization Plan**: `/docs/commercialization-plan.md`
- **V1.0 Baseline**: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.0/README.md`
- **BackOffice Setup**: `/backoffice/README.md`

## Support

Questions or issues?
1. Check main README: `/docs/04-how-to-docs.md`
2. Review commercialization plan: `/docs/commercialization-plan.md`
3. Open GitHub issue: https://github.com/techwayfit/twf-pulse/issues

---

**Version**: 1.1  
**Tables Added**: 3  
**Seed Rows**: 12  
**Status**: Production Ready ?
