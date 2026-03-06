# Migration V1.3: Promo Code Feature

## Overview

This migration adds support for promotional codes that grant temporary access to premium plans.

## Tables Created

### 1. `PromoCodes`
Stores promotional code definitions with validation rules and usage tracking.

**Columns:**
- `Id` - Primary key (GUID)
- `Code` - Unique promo code (uppercase, e.g., "LAUNCH2025")
- `TargetPlanId` - Plan to assign when redeemed (FK to `SubscriptionPlans`)
- `DurationDays` - Number of days the promo subscription lasts
- `MaxRedemptions` - Maximum uses (NULL = unlimited)
- `RedemptionsUsed` - Current usage count
- `ValidFrom` - Start date for code validity
- `ValidUntil` - Expiration date
- `IsActive` - Whether code can be redeemed
- `CreatedAt`, `UpdatedAt` - Timestamps

**Indexes:**
- Unique constraint on `Code`
- Composite index on `(IsActive, ValidFrom, ValidUntil)` for fast validation queries
- Index on `TargetPlanId` for plan lookups

### 2. `PromoCodeRedemptions`
Audit trail of promo code usage for analytics and fraud prevention.

**Columns:**
- `Id` - Primary key (GUID)
- `PromoCodeId` - FK to `PromoCodes`
- `FacilitatorUserId` - FK to `FacilitatorUsers`
- `SubscriptionId` - FK to `FacilitatorSubscriptions` (created by redemption)
- `RedeemedAt` - Timestamp
- `IpAddress` - User's IP (for fraud detection)

**Indexes:**
- Composite index on `(PromoCodeId, FacilitatorUserId)` for duplicate check
- Indexes on foreign keys for fast lookups

## Seed Data

3 sample promo codes:
1. **LAUNCH2025** - 30 days Plan A, max 100 uses, expires in 3 months
2. **FRIENDS50** - 60 days Plan B, unlimited uses, expires in 1 year
3. **SUPPORT2025** - 90 days Plan A, max 20 uses, expires in 6 months

## How to Apply

```bash
# From repository root
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.3/00_PromoCodeSchema.sql
```

## Rollback

```bash
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.3/99_Rollback.sql
```

?? **WARNING:** Rollback deletes all promo codes and redemption history!

## Dependencies

- Requires V1.1 (SubscriptionPlans, FacilitatorSubscriptions tables)
- Must run **after** V1.1 and V1.2 migrations

## Post-Migration Steps

1. Verify tables exist: `SHOW TABLES LIKE 'Promo%';`
2. Check seed data: `SELECT Code, DurationDays, MaxRedemptions FROM PromoCodes;`
3. Update `PulseMariaDbContext.cs` (add `DbSet<PromoCodeRecord>`)
4. Deploy BackOffice promo code management UI
5. Deploy main app promo redemption API

## Business Logic

**Validation Rules (enforced by `PromoCodeService`):**
- ? Code must be active (`IsActive = 1`)
- ? Current date within `ValidFrom` and `ValidUntil`
- ? `RedemptionsUsed < MaxRedemptions` (if limit set)
- ? User has not previously redeemed this code
- ? User must be authenticated

**Redemption Flow:**
1. User enters code in UI
2. API validates code
3. Old active subscription marked as expired
4. New time-limited subscription created with `ExpiresAt` set
5. Redemption recorded in `PromoCodeRedemptions`
6. Promo code usage counter incremented

**Auto-Expiration:**
- Background job checks `FacilitatorSubscriptions.ExpiresAt`
- Expired promo subscriptions automatically downgraded to Free plan

## Testing

```sql
-- Test promo code validation
SELECT 
    Code,
    CASE 
 WHEN NOT IsActive THEN '? Inactive'
        WHEN NOW() < ValidFrom THEN '? Not yet valid'
        WHEN NOW() > ValidUntil THEN '? Expired'
        WHEN MaxRedemptions IS NOT NULL AND RedemptionsUsed >= MaxRedemptions THEN '? Limit reached'
        ELSE '? Valid'
    END AS Status
FROM PromoCodes;

-- Test redemption tracking
SELECT 
    pc.Code,
    COUNT(pcr.Id) AS TotalRedemptions,
    COUNT(DISTINCT pcr.FacilitatorUserId) AS UniqueUsers
FROM PromoCodes pc
LEFT JOIN PromoCodeRedemptions pcr ON pc.Id = pcr.PromoCodeId
GROUP BY pc.Id, pc.Code;
```

## Analytics Queries

```sql
-- Most popular promo codes
SELECT 
    Code,
    RedemptionsUsed,
    (SELECT DisplayName FROM SubscriptionPlans WHERE Id = TargetPlanId) AS TargetPlan
FROM PromoCodes
WHERE RedemptionsUsed > 0
ORDER BY RedemptionsUsed DESC;

-- Promo code redemptions by date
SELECT 
    DATE(RedeemedAt) AS Date,
    COUNT(*) AS Redemptions
FROM PromoCodeRedemptions
WHERE RedeemedAt >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY DATE(RedeemedAt)
ORDER BY Date DESC;

-- User count per promo code
SELECT 
    pc.Code,
    COUNT(DISTINCT pcr.FacilitatorUserId) AS UniqueUsers,
    COUNT(pcr.Id) AS TotalRedemptions
FROM PromoCodes pc
LEFT JOIN PromoCodeRedemptions pcr ON pc.Id = pcr.PromoCodeId
GROUP BY pc.Id, pc.Code;
```
