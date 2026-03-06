# MariaDB/MySQL Schema Scripts - V1.2 Flexible Plan Access

## Overview

This directory contains MariaDB/MySQL schema upgrade scripts for **Version 1.2**, which removes the deprecated `MinPlanCode` field and adds flexible plan-based access control to ActivityTypeDefinitions.

## What's New in V1.2

### Schema Changes

**Removed:**
- `MinPlanCode` VARCHAR(50) column
- `FK_ActivityTypeDefinitions_MinPlanCode` foreign key constraint

**Added:**
- `ApplicablePlanIds` VARCHAR(500) - Pipe-separated list of plan GUIDs
- `IsAvailableToAllPlans` TINYINT(1) - Boolean flag for free/all-plan activities

### Why This Change?

The old `MinPlanCode` approach enforced a rigid tier hierarchy (e.g., "Plan A or higher"). The new approach provides:

? **Mix-and-match flexibility** - Assign activities to ANY combination of plans  
? **No tier hierarchy** - Plan B can have exclusive features not in Plan A  
? **Future-proof** - Easy to add new plans without schema changes  
? **A/B testing ready** - Create variant plans with different feature sets  

## Files in This Directory

| File | Purpose | When to Run |
|------|---------|-------------|
| `01_AddApplicablePlansToActivityTypes.sql` | **Migration from V1.1 to V1.2** | If you already have V1.1 installed |
| `README.md` | This file | Documentation |

## Prerequisites

- **V1.1 schema must be applied first**
- MariaDB 10.3+ or MySQL 8.0+
- Database: `pulse`
- User with ALTER TABLE permissions

## Quick Start

### Upgrade from V1.1 to V1.2

```bash
mysql -u pulseuser -p pulse < 01_AddApplicablePlansToActivityTypes.sql
```

**What it does:**
1. ? Adds `ApplicablePlanIds` and `IsAvailableToAllPlans` columns
2. ? Migrates existing data (free activities ? `IsAvailableToAllPlans=1`, premium ? applicable plan GUIDs)
3. ? Drops deprecated `MinPlanCode` column and foreign key
4. ? Prints verification report

**Expected output:**
```
? ApplicablePlanIds and IsAvailableToAllPlans columns added
? Deprecated MinPlanCode column removed
? Updated 7 free activities (available to all plans)
? Updated 2 premium activities (Plan A and Plan B access)

Verification Report:
  - Free activities: 7
  - Premium activities: 2
```

### Fresh Installation

If you're installing from scratch, use the updated **V1.1** script which already includes these changes:

```bash
mysql -u pulseuser -p pulse < ../V1.1/00_CommercializationSchema.sql
```

## Verification

After running the migration:

```sql
-- Check new columns exist
DESCRIBE ActivityTypeDefinitions;

-- Verify data migration
SELECT 
    DisplayName,
    RequiresPremium,
    IsAvailableToAllPlans,
    ApplicablePlanIds
FROM ActivityTypeDefinitions
WHERE IsActive = 1
ORDER BY SortOrder;
```

**Expected results:**

| DisplayName | RequiresPremium | IsAvailableToAllPlans | ApplicablePlanIds |
|-------------|-----------------|----------------------|-------------------|
| Poll | 0 | 1 | NULL |
| Word Cloud | 0 | 1 | NULL |
| Quadrant | 0 | 1 | NULL |
| Five Whys | 1 | 0 | guid1\|guid2 |
| Rating | 0 | 1 | NULL |
| Feedback | 0 | 1 | NULL |
| Q&A | 0 | 1 | NULL |
| AI Summary | 1 | 0 | guid1\|guid2 |
| Break | 0 | 1 | NULL |

## Usage Examples

### Make Activity Available to Specific Plans

```sql
-- Make "Custom Branding" available only to Plan B
UPDATE ActivityTypeDefinitions
SET IsAvailableToAllPlans = 0,
    ApplicablePlanIds = '00000000-0000-0000-0000-000000000003', -- Plan B GUID
    UpdatedAt = UTC_TIMESTAMP(6)
WHERE DisplayName = 'Custom Branding';
```

### Make Premium Activity Free for All

```sql
-- Make "Five Whys" available to all plans (promotion)
UPDATE ActivityTypeDefinitions
SET IsAvailableToAllPlans = 1,
    ApplicablePlanIds = NULL,
    RequiresPremium = 0,
    UpdatedAt = UTC_TIMESTAMP(6)
WHERE ActivityType = 6; -- FiveWhys
```

### Create Custom Plan with Exclusive Features

```sql
-- 1. Create "Pro" plan
INSERT INTO SubscriptionPlans VALUES (
    '00000000-0000-0000-0000-000000000004', 'pro', 'Professional',
    'For power users', 25.00, 250.00, 30,
    '{"aiAssist":true,"customBranding":true}',
    1, 4, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
);

-- 2. Make "Advanced Analytics" available to Plan B and Pro only
UPDATE ActivityTypeDefinitions
SET ApplicablePlanIds = '00000000-0000-0000-0000-000000000003|00000000-0000-0000-0000-000000000004'
WHERE DisplayName = 'Advanced Analytics';
```

## Impact on Existing Data

### ? Safe

- **No data loss** - All existing data preserved
- **Backward compatible** - Applications work without changes if using updated entity models
- **Idempotent** - Safe to re-run migration (uses `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` pattern)

### ?? Important Notes

1. **Code changes required**: Applications must update entity models to reference `ApplicablePlanIds` instead of `MinPlanCode`
2. **Cache invalidation**: If using in-memory cache for activity types, flush it after migration
3. **Documentation**: Update operator guides to reference new field names

## Troubleshooting

### "Key 'FK_ActivityTypeDefinitions_MinPlanCode' doesn't exist"

**Cause:** Foreign key was already dropped in a previous attempt  
**Solution:** Comment out the `DROP FOREIGN KEY` line and re-run

### "Column 'ApplicablePlanIds' already exists"

**Cause:** Migration was already partially applied
**Solution:** Migration is idempotent - check verification query to confirm data is correct

### "Cannot drop column 'MinPlanCode': needed in a foreign key constraint"

**Cause:** Foreign key wasn't dropped first  
**Solution:** Ensure foreign key drop runs before column drop (this is the correct order in the script)

## Rollback (Emergency Only)

?? **WARNING: This will lose the flexible plan assignment data!**

```sql
-- Add back MinPlanCode column
ALTER TABLE `ActivityTypeDefinitions`
ADD COLUMN `MinPlanCode` VARCHAR(50) NULL AFTER `RequiresPremium`;

-- Migrate data back (approximate - loses granular plan assignments)
UPDATE `ActivityTypeDefinitions`
SET `MinPlanCode` = 'plan-a'
WHERE `RequiresPremium` = 1 AND `IsAvailableToAllPlans` = 0;

-- Drop new columns
ALTER TABLE `ActivityTypeDefinitions`
DROP COLUMN `ApplicablePlanIds`,
DROP COLUMN `IsAvailableToAllPlans`;

-- Recreate foreign key
ALTER TABLE `ActivityTypeDefinitions`
ADD CONSTRAINT `FK_ActivityTypeDefinitions_MinPlanCode`
    FOREIGN KEY (`MinPlanCode`)
    REFERENCES `SubscriptionPlans`(`PlanCode`)
    ON UPDATE CASCADE;
```

**Use only if:**
- Migration caused unforeseen issues
- Application code was not updated properly
- **Never run in production without full backup**

## Related Documentation

- **Commercialization Plan**: `/docs/commercialization-plan.md`
- **Implementation Guide**: `/docs/flexible-plan-access-control-implementation.md`
- **V1.1 Baseline**: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.1/README.md`

---

**Version**: 1.2  
**Migration Type**: Additive + Removal  
**Downtime Required**: No (columns added/dropped, no data type changes)  
**Status**: Production Ready ?

