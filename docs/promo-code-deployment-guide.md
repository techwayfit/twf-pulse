# PromoCode Feature - Deployment Guide

## Prerequisites

? MariaDB 10.11+ running
? V1.0 schema (MasterSetup.sql) applied
? V1.1 schema (CommercializationSchema.sql) applied
? Application built successfully

## Step 1: Apply V1.3 Migration

```bash
# Navigate to migration scripts directory
cd src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.3

# Apply the promo code schema
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < 00_PromoCodeSchema.sql
```

### Expected Output:
```
================================================================
Starting V1.3 Migration: Promo Codes Feature
Date: 2026-07-03 04:32:15
================================================================

? PromoCodes table created/verified
? PromoCodeRedemptions table created/verified

Seeding sample promo codes...
  ? Seeded 3 promo codes

================================================================
Verification Report
================================================================
Table  Rows
------------------------+-----
PromoCodes           |    3
PromoCodeRedemptions |    0

Expected:
  - PromoCodes: 3 rows (sample codes)
  - PromoCodeRedemptions: 0 rows (populated on user redemption)

================================================================
Sample Promo Codes:
================================================================
Code   | Target Plan | Duration (Days) | Max Uses | Used | Expires
-------------+-------------+-----------------+----------+------+------------
FRIENDS50    | Plan B  |            60 |     9999 |    0 | 2027-07-03
LAUNCH2025   | Plan A      |       30 |      100 |    0 | 2026-10-03
SUPPORT2025  | Plan A      |   90 |    20 |    0 | 2027-01-03

================================================================
Migration V1.3 Completed Successfully
Date: 2026-07-03 04:32:15
================================================================
```

### Verify Migration:
```bash
# Check tables exist
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev -e "SHOW TABLES LIKE 'Promo%';"

# Check promo codes
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev -e "
SELECT Code, DurationDays, MaxRedemptions, RedemptionsUsed, IsActive 
FROM PromoCodes;"

# Check foreign keys
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev -e "
SELECT 
    CONSTRAINT_NAME, 
    TABLE_NAME, 
    REFERENCED_TABLE_NAME 
FROM information_schema.KEY_COLUMN_USAGE 
WHERE TABLE_SCHEMA = 'pulse_dev' 
  AND TABLE_NAME IN ('PromoCodes', 'PromoCodeRedemptions')
  AND REFERENCED_TABLE_NAME IS NOT NULL;"
```

---

## Step 2: Deploy Application Code

### Build All Projects

```bash
# From repository root
cd backoffice/src

# Clean previous builds
dotnet clean

# Build everything
dotnet build --no-incremental
```

### Expected Result:
```
Build succeeded with X warning(s) in Y.Ys

Projects built:
? TechWayFit.Pulse.Domain
? TechWayFit.Pulse.Contracts  
? TechWayFit.Pulse.Application
? TechWayFit.Pulse.Infrastructure
? TechWayFit.Pulse.BackOffice.Core
? TechWayFit.Pulse.BackOffice
```

---

## Step 3: Start Applications

### Start Main Web Application

```bash
cd ../../src/TechWayFit.Pulse.Web
dotnet run
```

**Verify:**
- App starts on `https://localhost:5001` (or configured port)
- Console log shows: `TechWayFit Pulse application started successfully`

### Start BackOffice Application

```bash
# In a new terminal
cd backoffice/src/TechWayFit.Pulse.BackOffice
dotnet run
```

**Verify:**
- BackOffice starts on `https://localhost:7001` (or configured port)
- No startup errors in logs

---

## Step 4: Test the Feature

### 4.1 Test BackOffice Promo Code Management

1. **Login to BackOffice:**
   - Navigate to `https://localhost:7001`
   - Login with SuperAdmin credentials

2. **View Promo Codes:**
   - Click "Promo Codes" in sidebar
   - Should see 3 codes: LAUNCH2025, FRIENDS50, SUPPORT2025
   - Verify Status, Duration, Max Uses columns

3. **View Code Details:**
   - Click "View" on LAUNCH2025
   - Should see:
     - Target Plan: Plan A
     - Duration: 30 days
     - Max Redemptions: 100
     - Redemptions Used: 0
     - Valid period
     - Active status
   - Should see "No redemptions yet" message

4. **Create New Promo Code:**
   - Click "+ New Promo Code"
   - Fill form:
     - Code: TEST2026
     - Target Plan: Plan B
  - Duration: 45 days
     - Max Redemptions: 50
     - Valid From: Today
     - Valid Until: +90 days
     - Active: Checked
   - Click "Create Promo Code"
   - Should redirect to detail page
   - Should see success message

5. **Edit Promo Code:**
   - Click "Edit" on TEST2026
   - Change Max Redemptions to 75
   - Enter Reason: "Increased demand"
   - Click "Save Changes"
   - Should see audit trail in Audit Log

### 4.2 Test API Endpoints

**Prerequisites:**
- Have a facilitator account and be authenticated
- Get auth token from browser cookies or login flow

#### Validate Promo Code (Without Redeeming)

```bash
curl -X POST https://localhost:5001/api/promo-codes/validate \
  -H "Content-Type: application/json" \
  -H "Cookie: TechWayFit.Pulse.Auth=YOUR_AUTH_COOKIE" \
  -d '{"code": "LAUNCH2025"}'
```

**Expected Response:**
```json
{
  "data": {
    "isValid": true,
    "errorMessage": null,
    "targetPlanDisplayName": "Plan A",
    "durationDays": 30
  },
  "errors": null
}
```

#### Test Invalid Code:

```bash
curl -X POST https://localhost:5001/api/promo-codes/validate \
  -H "Content-Type: application/json" \
  -H "Cookie: TechWayFit.Pulse.Auth=YOUR_AUTH_COOKIE" \
  -d '{"code": "INVALID123"}'
```

**Expected Response:**
```json
{
  "data": {
    "isValid": false,
    "errorMessage": "Promo code does not exist.",
    "targetPlanDisplayName": null,
    "durationDays": null
  },
  "errors": [
    {
      "code": "invalid_code",
      "message": "Promo code does not exist."
    }
  ]
}
```

#### Redeem Promo Code:

```bash
curl -X POST https://localhost:5001/api/promo-codes/redeem \
  -H "Content-Type: application/json" \
  -H "Cookie: TechWayFit.Pulse.Auth=YOUR_AUTH_COOKIE" \
  -d '{"code": "LAUNCH2025"}'
```

**Expected Response:**
```json
{
  "data": {
    "subscriptionId": "guid-here",
    "planId": "guid-here",
    "expiresAt": "2026-08-02T12:00:00Z",
    "message": "Promo code applied! You now have premium access until Aug 02, 2026."
  },
  "errors": null
}
```

#### Test Duplicate Redemption:

```bash
# Try to redeem same code again
curl -X POST https://localhost:5001/api/promo-codes/redeem \
  -H "Content-Type: application/json" \
  -H "Cookie: TechWayFit.Pulse.Auth=YOUR_AUTH_COOKIE" \
  -d '{"code": "LAUNCH2025"}'
```

**Expected Response:**
```json
{
  "data": null,
  "errors": [
 {
      "code": "invalid_code",
      "message": "You have already redeemed this promo code."
    }
  ]
}
```

### 4.3 Verify Database Changes

```sql
-- Check redemption was recorded
SELECT * FROM PromoCodeRedemptions ORDER BY RedeemedAt DESC LIMIT 5;

-- Check counter was incremented
SELECT Code, RedemptionsUsed, MaxRedemptions 
FROM PromoCodes 
WHERE Code = 'LAUNCH2025';

-- Check user's subscription
SELECT fs.*, sp.DisplayName as PlanName
FROM FacilitatorSubscriptions fs
JOIN SubscriptionPlans sp ON fs.PlanId = sp.Id
WHERE fs.FacilitatorUserId = 'YOUR_USER_ID'
ORDER BY fs.CreatedAt DESC
LIMIT 3;

-- Check audit trail
SELECT * FROM AuditLogs 
WHERE EntityType = 'PromoCode' 
ORDER BY OccurredAt DESC 
LIMIT 10;
```

---

## Step 5: Verification Checklist

### Backend Verification

- [ ] Migration script ran without errors
- [ ] 3 seed promo codes exist in database
- [ ] All foreign key constraints created
- [ ] All indexes created
- [ ] Main Web app starts without errors
- [ ] BackOffice app starts without errors
- [ ] Both apps connect to MariaDB successfully

### API Verification

- [ ] `/api/promo-codes/validate` requires authentication (401 if not authenticated)
- [ ] Valid code returns `isValid: true` with plan details
- [ ] Invalid code returns `isValid: false` with error message
- [ ] `/api/promo-codes/redeem` creates subscription
- [ ] Redemption increments `RedemptionsUsed` counter
- [ ] Duplicate redemption is rejected
- [ ] IP address is captured in redemption record
- [ ] Old subscription is expired when new promo subscription is created

### BackOffice Verification

- [ ] "Promo Codes" appears in sidebar navigation
- [ ] Index page shows 3 seed codes
- [ ] Status filter works (Active/Inactive/All)
- [ ] Validity filter works (Valid/Expired/All)
- [ ] Detail page shows usage statistics
- [ ] Create form validates inputs
- [ ] Create form submits successfully
- [ ] New code appears in list immediately
- [ ] Edit form pre-populates values
- [ ] Edit form prevents changing plan/duration for redeemed codes
- [ ] Toggle Active/Inactive works
- [ ] Delete works for unused codes
- [ ] Delete is blocked for redeemed codes
- [ ] Redemptions history page shows user emails and IPs
- [ ] All operations create audit log entries

### Security Verification

- [ ] Unauthenticated API requests return 401
- [ ] BackOffice requires SuperAdmin role
- [ ] Operators cannot access promo code pages (403)
- [ ] IP addresses are correctly captured
- [ ] Audit trail includes operator ID and reason
- [ ] Password/secrets not logged or exposed

---

## Troubleshooting

### Issue: "PromoCode does not exist" error immediately after creation

**Cause:** Cache consistency issue or code normalization mismatch

**Fix:**
```sql
-- Check how code is stored
SELECT Code FROM PromoCodes WHERE Code LIKE '%LAUNCH%';

-- PromoCodeService normalizes to uppercase
-- Ensure BackOffice also uppercases on create
```

### Issue: "Promo code already exists" when creating unique code

**Cause:** Unique constraint on `Code` column, case-insensitive collation

**Fix:**
```sql
-- Check collation
SHOW FULL COLUMNS FROM PromoCodes WHERE Field = 'Code';

-- Should be utf8mb4_unicode_ci (case-insensitive)
```

### Issue: Foreign key violation on redemption

**Cause:** Target plan does not exist or user has no FacilitatorUser record

**Fix:**
```sql
-- Check plan exists
SELECT * FROM SubscriptionPlans WHERE Id = 'PLAN_ID_FROM_PROMOCODE';

-- Check user exists
SELECT * FROM FacilitatorUsers WHERE Id = 'USER_ID_FROM_AUTH';
```

### Issue: BackOffice shows "Unknown Plan"

**Cause:** Target plan ID doesn't match any plan in database

**Fix:**
```sql
-- Check orphaned promo codes
SELECT pc.Code, pc.TargetPlanId, sp.DisplayName
FROM PromoCodes pc
LEFT JOIN SubscriptionPlans sp ON pc.TargetPlanId = sp.Id
WHERE sp.Id IS NULL;

-- Fix by updating to valid plan or deleting invalid codes
```

###Issue: "Table 'PromoCodes' doesn't exist"

**Cause:** Migration not applied

**Fix:**
```bash
# Re-run migration
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < 00_PromoCodeSchema.sql
```

### Issue: Build errors about PromoCodeRecord not found

**Cause:** Infrastructure DLL not rebuilt or Visual Studio caching stale references

**Fix:**
```bash
# Clean and rebuild Infrastructure first
dotnet clean ../../src/TechWayFit.Pulse.Infrastructure/
dotnet build ../../src/TechWayFit.Pulse.Infrastructure/ --no-incremental

# Then rebuild BackOffice
dotnet clean TechWayFit.Pulse.BackOffice.Core/
dotnet build TechWayFit.Pulse.BackOffice.Core/ --no-incremental
```

---

## Rollback Procedure

If you need to rollback the feature:

```bash
# Run rollback script
cd src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.3
mysql -h 127.0.0.1 -P 3306 -u root -pdevpassword pulse_dev < 99_Rollback.sql
```

**?? WARNING:** Rollback will:
- Drop `PromoCodeRedemptions` table (losing all redemption history)
- Drop `PromoCodes` table (losing all promo codes)
- Any subscriptions created by promo codes will remain (orphaned)

**Alternative (Safer):** Instead of dropping tables, deactivate all codes:
```sql
UPDATE PromoCodes SET IsActive = 0;
```

---

## Monitoring & Operations

### Daily Health Checks

```sql
-- Check promo code usage (daily)
SELECT 
    Code,
    RedemptionsUsed,
    MaxRedemptions,
    ValidUntil,
    CASE 
        WHEN ValidUntil < NOW() THEN 'Expired'
        WHEN MaxRedemptions IS NOT NULL AND RedemptionsUsed >= MaxRedemptions THEN 'Limit Reached'
      WHEN IsActive = 0 THEN 'Inactive'
 ELSE 'Active'
    END AS Status
FROM PromoCodes
WHERE IsActive = 1
ORDER BY ValidUntil;

-- Check recent redemptions
SELECT 
    pc.Code,
    COUNT(*) as TodayRedemptions
FROM PromoCodeRedemptions pcr
JOIN PromoCodes pc ON pcr.PromoCodeId = pc.Id
WHERE DATE(pcr.RedeemedAt) = CURDATE()
GROUP BY pc.Code
ORDER BY TodayRedemptions DESC;

-- Check codes nearing limit
SELECT 
    Code,
    RedemptionsUsed,
    MaxRedemptions,
    (MaxRedemptions - RedemptionsUsed) AS Remaining
FROM PromoCodes
WHERE MaxRedemptions IS NOT NULL
  AND IsActive = 1
  AND RedemptionsUsed >= (MaxRedemptions * 0.8) -- 80% threshold
ORDER BY Remaining;
```

### Weekly Analytics

```sql
-- Top codes by redemptions (last 7 days)
SELECT 
    pc.Code,
    COUNT(pcr.Id) AS Redemptions,
    COUNT(DISTINCT pcr.FacilitatorUserId) AS UniqueUsers,
    sp.DisplayName AS TargetPlan
FROM PromoCodes pc
LEFT JOIN PromoCodeRedemptions pcr ON pc.Id = pcr.PromoCodeId
    AND pcr.RedeemedAt >= DATE_SUB(NOW(), INTERVAL 7 DAY)
LEFT JOIN SubscriptionPlans sp ON pc.TargetPlanId = sp.Id
GROUP BY pc.Code, sp.DisplayName
ORDER BY Redemptions DESC;

-- Conversion rate by code
SELECT 
    Code,
    RedemptionsUsed,
    MaxRedemptions,
    ROUND((RedemptionsUsed * 100.0 / MaxRedemptions), 2) AS ConversionRate
FROM PromoCodes
WHERE MaxRedemptions IS NOT NULL
  AND RedemptionsUsed > 0
ORDER BY ConversionRate DESC;

-- Revenue impact (estimate)
SELECT 
    pc.Code,
    COUNT(pcr.Id) AS Redemptions,
    sp.PriceMonthly,
    (COUNT(pcr.Id) * sp.PriceMonthly * (pc.DurationDays / 30.0)) AS EstimatedRevenue
FROM PromoCodes pc
LEFT JOIN PromoCodeRedemptions pcr ON pc.Id = pcr.PromoCodeId
JOIN SubscriptionPlans sp ON pc.TargetPlanId = sp.Id
GROUP BY pc.Code, sp.PriceMonthly, pc.DurationDays
ORDER BY EstimatedRevenue DESC;
```

### Alerts to Set Up

1. **Code near limit**: Alert when code reaches 90% of MaxRedemptions
2. **High redemption rate**: Alert if >10 redemptions/hour (potential abuse)
3. **Expired codes still active**: Alert if `ValidUntil < NOW()` but `IsActive = 1`
4. **Foreign key violations**: Monitor error logs for FK constraint errors

---

## Common Operations

### Extend Code Validity

```sql
-- Extend LAUNCH2025 by 30 days
UPDATE PromoCodes 
SET ValidUntil = DATE_ADD(ValidUntil, INTERVAL 30 DAY),
  UpdatedAt = UTC_TIMESTAMP(6)
WHERE Code = 'LAUNCH2025';
```

(Better: Use BackOffice Edit form for audit trail)

### Increase Redemption Limit

```sql
-- Increase FRIENDS50 to 200 uses
UPDATE PromoCodes 
SET MaxRedemptions = 200,
    UpdatedAt = UTC_TIMESTAMP(6)
WHERE Code = 'FRIENDS50';
```

(Better: Use BackOffice Edit form for audit trail)

### Deactivate Campaign

```sql
-- End campaign early
UPDATE PromoCodes 
SET IsActive = 0,
    UpdatedAt = UTC_TIMESTAMP(6)
WHERE Code = 'LAUNCH2025';
```

(Better: Use BackOffice Toggle Active for audit trail)

### Export Redemption Data

```sql
-- Export to CSV
SELECT 
    pc.Code,
    fu.Email AS UserEmail,
    pcr.RedeemedAt,
    pcr.IpAddress,
    fs.ExpiresAt AS SubscriptionExpires
FROM PromoCodeRedemptions pcr
JOIN PromoCodes pc ON pcr.PromoCodeId = pc.Id
JOIN FacilitatorUsers fu ON pcr.FacilitatorUserId = fu.Id
JOIN FacilitatorSubscriptions fs ON pcr.SubscriptionId = fs.Id
WHERE pcr.RedeemedAt >= '2026-01-01'
ORDER BY pcr.RedeemedAt DESC
INTO OUTFILE '/tmp/promo-redemptions.csv'
FIELDS TERMINATED BY ',' ENCLOSED BY '"'
LINES TERMINATED BY '\n';
```

---

## Security Considerations

### IP Address Privacy

PromoCode redemptions store IP addresses for fraud detection. Ensure compliance with GDPR:

```sql
-- Anonymize IP addresses after 90 days (GDPR compliance)
UPDATE PromoCodeRedemptions
SET IpAddress = 'anonymized'
WHERE RedeemedAt < DATE_SUB(NOW(), INTERVAL 90 DAY)
  AND IpAddress != 'anonymized';
```

Consider adding this to a scheduled job.

### Prevent Brute Force

Add rate limiting to API endpoints (future enhancement):
```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("promo-redeem", o =>
    {
        o.PermitLimit = 5; // 5 redemption attempts
        o.Window = TimeSpan.FromMinutes(10); // per 10 minutes
        o.QueueLimit = 0;
    });
});

// On controller action
[EnableRateLimiting("promo-redeem")]
[HttpPost("redeem")]
public async Task<ActionResult> RedeemCode(...) { ... }
```

---

## Performance Monitoring

### Key Metrics to Track

1. **Redemption Latency**: `/api/promo-codes/redeem` response time
   - Target: < 200ms
   - Alert if > 1s

2. **Validation Latency**: `/api/promo-codes/validate` response time
   - Target: < 100ms
   - Alert if > 500ms

3. **Database Query Performance**:
   ```sql
   -- Check slow queries
   SHOW PROFILES;
   
   -- Verify indexes are used
 EXPLAIN SELECT * FROM PromoCodes WHERE Code = 'LAUNCH2025' AND IsActive = 1;
   -- Should use UQ_PromoCodes_Code index
   ```

4. **Redemption Success Rate**:
   - % of validate calls that lead to successful redemptions
   - Track via application logs

---

## Success Criteria

? Migration applies cleanly without errors
? All 3 sample codes visible in BackOffice
? New codes can be created via BackOffice
? `/api/promo-codes/validate` returns correct validation results
? `/api/promo-codes/redeem` creates time-limited subscriptions
? Duplicate redemptions are prevented
? RedemptionsUsed counter increments correctly
? Audit trail captures all BackOffice operations
? No errors in application logs during testing
? Database foreign keys enforce data integrity

---

## Next Steps After Deployment

1. **Monitor redemption rates** for first 48 hours
2. **Check for abuse patterns** (same IP multiple redemptions with different accounts)
3. **Gather feedback** from support team on BackOffice UX
4. **Document common support scenarios** (code not working, already redeemed, etc.)
5. **Plan frontend integration** (add promo code input to signup/upgrade flows)
6. **Set up email notifications** for successful redemptions
7. **Create marketing materials** with promo code instructions

---

## Support Scenarios

### User Reports "Code Not Working"

**Diagnosis:**
1. Check code exists: `SELECT * FROM PromoCodes WHERE Code = 'USER_CODE';`
2. Check IsActive: Should be 1
3. Check ValidFrom/ValidUntil: Current time must be within range
4. Check MaxRedemptions: `RedemptionsUsed < MaxRedemptions`
5. Check user hasn't already redeemed: Query `PromoCodeRedemptions`

**Resolution:**
- If code expired: Extend ValidUntil via BackOffice
- If limit reached: Increase MaxRedemptions via BackOffice
- If already redeemed: Explain one-time use policy, offer different code
- If inactive: Activate via BackOffice (if campaign should be active)

### Operator Wants to Comp a User

**Process:**
1. Create new promo code in BackOffice:
   - Code: COMP-USERID-2026 (unique per user)
   - Max Redemptions: 1
   - Valid: Short window (1 week)
   - Duration: 90 days (or custom)
2. Send code directly to user (email/support ticket)
3. Monitor redemption in BackOffice
4. Deactivate code after use

### Campaign Ends Early

**Process:**
1. Navigate to Promo Codes ? Find code ? Detail
2. Click Deactivate
3. Enter Reason: "Campaign goals met early"
4. Confirm

Existing redemptions remain valid until their expiration dates.

---

## Files Modified/Created Summary

### New Files (20 total):

**Domain (2):**
- `Domain/Entities/PromoCode.cs`
- `Domain/Entities/PromoCodeRedemption.cs`

**Infrastructure (7):**
- `Persistence/Entities/PromoCodeRecord.cs`
- `Persistence/Entities/PromoCodeRedemptionRecord.cs`
- `Persistence/Repositories/PromoCodeRepository.cs`
- `Persistence/Repositories/SubscriptionPlanRepository.cs`
- `Persistence/Repositories/FacilitatorSubscriptionRepository.cs`
- `Scripts/MariaDB/V1.3/00_PromoCodeSchema.sql`
- `Scripts/MariaDB/V1.3/99_Rollback.sql`
- `Scripts/MariaDB/V1.3/README.md`

**Application (3):**
- `Abstractions/Repositories/IPromoCodeRepository.cs`
- `Abstractions/Services/IPromoCodeService.cs`
- `Services/PromoCodeService.cs`

**Contracts (2):**
- `Requests/PromoCodeRequests.cs`
- `Responses/PromoCodeResponses.cs`

**Web (1):**
- `Controllers/Api/PromoCodesController.cs`

**BackOffice (5):**
- `Controllers/PromoCodesController.cs`
- `Views/PromoCodes/Index.cshtml`
- `Views/PromoCodes/Create.cshtml`
- `Views/PromoCodes/Detail.cshtml`
- `Views/PromoCodes/Edit.cshtml`
- `Views/PromoCodes/Redemptions.cshtml`

**BackOffice.Core (2):**
- `Abstractions/IBackOfficePromoCodeService.cs`
- `Services/BackOfficePromoCodeService.cs`

**Documentation (2):**
- `docs/promo-code-implementation-summary.md`
- `docs/promo-code-deployment-guide.md` (this file)

### Modified Files (6):

- `Infrastructure/Persistence/MariaDb/PulseMariaDbContext.cs` - Added PromoCode DbSets
- `Infrastructure/Extensions/DatabaseServiceExtensions.cs` - Registered repositories
- `Application/Abstractions/Repositories/IFacilitatorSubscriptionRepository.cs` - Added methods
- `Application/Abstractions/Repositories/ISubscriptionPlanRepository.cs` - Added methods
- `Web/Program.cs` - Registered IPromoCodeService
- `BackOffice.Core/BackOfficeCoreServiceExtensions.cs` - Registered IBackOfficePromoCodeService
- `BackOffice.Core/Models/Commercialization/CommercializationModels.cs` - Added PromoCode models
- `BackOffice.Core/Persistence/MariaDb/BackOfficeMariaDbContext.cs` - Added PromoCode DbSets/config
- `BackOffice/Views/Shared/_Layout.cshtml` - Added navigation link

---

## Production Checklist

Before deploying to production:

- [ ] Backup database
- [ ] Test rollback procedure in staging
- [ ] Verify all seed codes have correct expiration dates
- [ ] Update MaxRedemptions for production campaign scale
- [ ] Set up monitoring alerts for redemption anomalies
- [ ] Document support procedures for common user issues
- [ ] Train support team on BackOffice promo code management
- [ ] Prepare marketing announcement with code instructions
- [ ] Set up analytics dashboard for campaign tracking
- [ ] Enable rate limiting on redemption endpoint
- [ ] Add logging for all redemption attempts (success + failure)
- [ ] Test with staging payment processor (if Paddle integrated)
- [ ] Verify tax compliance for promotional subscriptions
- [ ] Review GDPR compliance for IP address storage
- [ ] Set up automated IP anonymization job (90-day retention)

---

## ?? Deployment Complete!

Your PromoCode feature is now live and ready to drive user acquisition campaigns.

**Next:** Integrate promo code input into frontend signup/upgrade flows (Phase 2).
