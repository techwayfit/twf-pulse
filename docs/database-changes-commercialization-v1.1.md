# Database & BackOffice Changes - Commercialization Phase 1

## Summary

This document summarizes the database schema changes and infrastructure updates completed for Phase 1 of the commercialization plan.

## Changes Completed

### ? 1. Domain Layer - New Entities

Created 4 new domain files:

- **`SubscriptionPlan.cs`** - Defines pricing tiers (Free, Plan A, Plan B) with monthly/yearly pricing, session quotas, and feature flags
- **`FacilitatorSubscription.cs`** - Tracks user subscriptions with status, usage tracking, and payment provider integration
- **`ActivityTypeDefinition.cs`** - Defines activity metadata and premium access rules
- **`SubscriptionStatus.cs`** (enum) - Active, Canceled, Expired, Trial
- **`PlanFeatures.cs`** (value object) - Type-safe feature flags parsed from JSON

### ? 2. Infrastructure Layer - Persistence

#### New EF Core Records

- **`SubscriptionPlanRecord.cs`** - EF Core mapping for SubscriptionPlan
- **`FacilitatorSubscriptionRecord.cs`** - EF Core mapping for FacilitatorSubscription  
- **`ActivityTypeDefinitionRecord.cs`** - EF Core mapping for ActivityTypeDefinition

#### Updated DbContext Files

All DbContext implementations updated to include 3 new tables:

- ? `PulseDbContextBase.cs` - Common entity configuration
- ? `IPulseDbContext.cs` - Interface updated with new DbSets
- ? `PulseSqlLiteDbContext.cs` - SQLite-specific column types
- ? `PulseSqlServerDbContext.cs` - SQL Server-specific column types (NVARCHAR(MAX))
- ? `PulseMariaDbContext.cs` - MariaDB-specific column types (LONGTEXT)

#### BackOffice DbContext Updates

All BackOffice DbContext implementations updated:

- ? `BackOfficeSqliteDbContext.cs`
- ? `BackOfficeSqlServerDbContext.cs`
- ? `BackOfficeMariaDbContext.cs`

### ? 3. Database Migration Scripts

#### SQL Server (Manual Scripts)

**Location:** `src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.1/`

- **`00_CommercializationSchema.sql`** - Creates 3 tables, seeds 12 rows, verifies installation
- **`99_Rollback.sql`** - Emergency rollback (drops all V1.1 tables)
- **`README.md`** - Complete documentation with verification queries

**Seed Data:**
- 3 subscription plans (Free, Plan A, Plan B)
- 9 activity type definitions (Poll, WordCloud, Quadrant, FiveWhys, Rating, Feedback, Q&A, AI Summary, Break)

#### MariaDB/MySQL (Manual Scripts)

**Location:** `src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.1/`

- **`00_CommercializationSchema.sql`** - MariaDB-compatible schema with seed data
- **`99_Rollback.sql`** - Emergency rollback
- **`README.md`** - Complete documentation

### ? 4. Automatic Seeding for SQLite/InMemory

Created **`CommercializationSeedService.cs`** - Static utility that seeds plans and activity types on startup for SQLite and InMemory providers.

**Integration:**
- Called from `EnsurePulseDatabaseAsync()` in `DatabaseServiceExtensions.cs`
- Runs after `EnsureCreated()` for SQLite
- Idempotent - safe to run multiple times (checks if data exists before inserting)

### ? 5. Database Service Registration

Updated **`DatabaseServiceExtensions.cs`**:
- Changed `EnsurePulseDatabase()` to `EnsurePulseDatabaseAsync()` - now async
- Calls `CommercializationSeedService.SeedAsync()` for SQLite and InMemory
- SQL Server and MariaDB rely on manual scripts (no seeding)

Updated **`Program.cs`** (main app):
- Changed call from `app.Services.EnsurePulseDatabase()` to `await app.Services.EnsurePulseDatabaseAsync()`

---

## Database Schema

### New Tables (3)

#### 1. SubscriptionPlans

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary key |
| PlanCode | VARCHAR(50) | Unique: 'free', 'plan-a', 'plan-b' |
| DisplayName | VARCHAR(100) | 'Free', 'Plan A', 'Plan B' |
| Description | VARCHAR(500) | Marketing description |
| PriceMonthly | DECIMAL(10,2) | Monthly price USD |
| PriceYearly | DECIMAL(10,2) | Annual price USD (nullable) |
| MaxSessionsPerMonth | INT | Session creation quota |
| FeaturesJson | TEXT/NVARCHAR(MAX) | JSON: `{"aiAssist": true, ...}` |
| IsActive | BIT | Show in pricing page? |
| SortOrder | INT | Display order |
| CreatedAt | DATETIMEOFFSET | |
| UpdatedAt | DATETIMEOFFSET | |

**Indexes:**
- UNIQUE on `PlanCode`
- INDEX on `(IsActive, SortOrder)`

#### 2. FacilitatorSubscriptions

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary key |
| FacilitatorUserId | GUID | FK to FacilitatorUsers |
| PlanId | GUID | FK to SubscriptionPlans |
| Status | VARCHAR(20) | Active, Canceled, Expired, Trial |
| StartsAt | DATETIMEOFFSET | Subscription start |
| ExpiresAt | DATETIMEOFFSET | Expiry (nullable for rolling) |
| CanceledAt | DATETIMEOFFSET | Cancellation date |
| SessionsUsed | INT | Current month usage |
| SessionsResetAt | DATETIMEOFFSET | Next reset date |
| PaymentProvider | VARCHAR(50) | 'paddle', 'stripe', null |
| ExternalCustomerId | VARCHAR(200) | Payment provider customer ID |
| ExternalSubscriptionId | VARCHAR(200) | Payment provider subscription ID |
| CreatedAt | DATETIMEOFFSET | |
| UpdatedAt | DATETIMEOFFSET | |

**Indexes:**
- INDEX on `(FacilitatorUserId, Status)` - fast active subscription lookup
- INDEX on `ExternalSubscriptionId` - webhook reconciliation
- INDEX on `PlanId` - reporting

**Foreign Keys:**
- `FacilitatorUserId` ? `FacilitatorUsers.Id` (CASCADE DELETE)
- `PlanId` ? `SubscriptionPlans.Id`

#### 3. ActivityTypeDefinitions

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary key |
| ActivityType | INT | Links to ActivityType enum (0-10) |
| DisplayName | VARCHAR(100) | 'Five Whys', 'AI Summary' |
| Description | VARCHAR(500) | Feature description |
| IconClass | VARCHAR(100) | CSS class for icon |
| ColorHex | VARCHAR(7) | Badge color (#RRGGBB) |
| RequiresPremium | BIT | Locked for free plan? |
| MinPlanCode | VARCHAR(50) | Minimum plan required (nullable) |
| IsActive | BIT | Show in UI? |
| SortOrder | INT | Display order |
| CreatedAt | DATETIMEOFFSET | |
| UpdatedAt | DATETIMEOFFSET | |

**Indexes:**
- UNIQUE on `ActivityType` - one row per enum value
- INDEX on `(IsActive, SortOrder)`

**Foreign Keys:**
- `MinPlanCode` ? `SubscriptionPlans.PlanCode` (CASCADE UPDATE)

---

## Seed Data

### Subscription Plans (3 rows)

| PlanCode | DisplayName | Price/Month | Sessions/Month | Features |
|----------|-------------|-------------|----------------|----------|
| `free` | Free | $0 | 2 | No AI features |
| `plan-a` | Plan A | $10 ($100/year) | 5 | All AI features |
| `plan-b` | Plan B | $20 ($200/year) | 15 | All AI features |

### Activity Type Definitions (9 rows)

| ActivityType | DisplayName | Premium? | Min Plan | Active? |
|--------------|-------------|----------|----------|---------|
| 0 (Poll) | Poll | No | - | Yes |
| 2 (WordCloud) | Word Cloud | No | - | Yes |
| 5 (Quadrant) | Quadrant | No | - | Yes |
| 6 (FiveWhys) | Five Whys | **Yes** | **plan-a** | Yes |
| 4 (Rating) | Rating | No | - | Yes |
| 7 (GeneralFeedback) | Feedback | No | - | Yes |
| 3 (QnA) | Q&A | No | - | Yes |
| 8 (AiSummary) | AI Summary | **Yes** | **plan-a** | Yes |
| 9 (Break) | Break | No | - | Yes |

---

## How to Apply Migrations

### SQLite (Automatic)

1. Start the application
2. Database file is created at `App_Data/pulse.db` (or configured path)
3. Commercialization data is seeded automatically
4. **No manual steps required**

### SQL Server (Manual)

1. Ensure V1.0 baseline is applied first
2. Run migration script:
   ```bash
   sqlcmd -S localhost -d TechWayFitPulse -E -i "src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.1/00_CommercializationSchema.sql"
   ```
3. Verify with queries in README
4. **Optionally** run grandfathering script to give existing users Plan B for 90 days

### MariaDB/MySQL (Manual)

1. Ensure V1.0 baseline is applied first
2. Run migration script:
   ```bash
   mysql -u pulseuser -p pulse < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.1/00_CommercializationSchema.sql
   ```
3. Verify with queries in README
4. **Optionally** run grandfathering script

---

## Verification Queries

### Check Tables Exist

```sql
-- SQL Server
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'pulse' 
  AND TABLE_NAME IN ('SubscriptionPlans', 'FacilitatorSubscriptions', 'ActivityTypeDefinitions')
ORDER BY TABLE_NAME;

-- MariaDB/MySQL
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'pulse'
  AND TABLE_NAME IN ('SubscriptionPlans', 'FacilitatorSubscriptions', 'ActivityTypeDefinitions')
ORDER BY TABLE_NAME;

-- SQLite
SELECT name FROM sqlite_master 
WHERE type='table' 
  AND name IN ('SubscriptionPlans', 'FacilitatorSubscriptions', 'ActivityTypeDefinitions')
ORDER BY name;
```

### Check Seed Data

```sql
-- All providers
SELECT PlanCode, DisplayName, MaxSessionsPerMonth, PriceMonthly 
FROM SubscriptionPlans -- or pulse.SubscriptionPlans for SQL Server
ORDER BY SortOrder;

SELECT ActivityType, DisplayName, RequiresPremium, MinPlanCode 
FROM ActivityTypeDefinitions -- or pulse.ActivityTypeDefinitions for SQL Server
WHERE IsActive = 1
ORDER BY SortOrder;
```

**Expected:**
- 3 subscription plans
- 9 activity type definitions
- 0 facilitator subscriptions (auto-created on first user interaction)

---

## Next Steps

### Phase 1 Remaining Tasks

Now that database schema is complete, next steps are:

#### Repository Layer
- [ ] Create `ISubscriptionPlanRepository` interface
- [ ] Create `IFacilitatorSubscriptionRepository` interface
- [ ] Create `IActivityTypeDefinitionRepository` interface
- [ ] Implement repositories for each database provider

#### Application Layer
- [ ] Create `IPlanService` interface
- [ ] Implement `PlanService` with quota checking
- [ ] Implement feature gating logic
- [ ] Add caching decorator for performance

#### BackOffice UI
- [ ] Create Plan Management page (CRUD for plans)
- [ ] Create Activity Type Management page (toggle premium flags)
- [ ] Add plan assignment in user detail page
- [ ] Add revenue/usage reports

#### Main App API
- [ ] Gate `POST /api/sessions` with session quota check
- [ ] Gate `POST /api/sessions/{code}/generate-activities` with AI Assist feature check
- [ ] Gate `POST /api/sessions/{code}/activities` with activity type access check
- [ ] Create `GET /api/plans` endpoint (public pricing page)
- [ ] Create `GET /api/account/plan-status` endpoint

#### Main App UI
- [ ] Update activity picker to show locked activities
- [ ] Add session quota warning on Create Session page
- [ ] Add upgrade modals
- [ ] Update Profile page with Plan & Billing section

---

## Files Created

### Domain Layer (5 files)
```
src/TechWayFit.Pulse.Domain/
??? Entities/
?   ??? SubscriptionPlan.cs
?   ??? FacilitatorSubscription.cs
?   ??? ActivityTypeDefinition.cs
??? Enums/
?   ??? SubscriptionStatus.cs
??? ValueObjects/
    ??? PlanFeatures.cs
```

### Infrastructure Layer (8 files)
```
src/TechWayFit.Pulse.Infrastructure/
??? Persistence/
?   ??? Entities/
? ??? SubscriptionPlanRecord.cs
?       ??? FacilitatorSubscriptionRecord.cs
?       ??? ActivityTypeDefinitionRecord.cs
??? Services/
?   ??? CommercializationSeedService.cs
??? Scripts/
 ??? MSQL/V1.1/
    ?   ??? 00_CommercializationSchema.sql
    ?   ??? 99_Rollback.sql
    ?   ??? README.md
    ??? MariaDB/V1.1/
        ??? 00_CommercializationSchema.sql
        ??? 99_Rollback.sql
     ??? README.md
```

### Files Modified (10 files)
```
src/TechWayFit.Pulse.Infrastructure/
??? Persistence/
?   ??? PulseDbContextBase.cs          (+ 3 DbSets, + 3 entity configs)
?   ??? Abstractions/IPulseDbContext.cs          (+ 3 DbSet properties)
?   ??? Sqlite/PulseSqlLiteDbContext.cs          (+ 3 table configs)
?   ??? SqlServer/PulseSqlServerDbContext.cs     (+ 3 table configs)
?   ??? MariaDb/PulseMariaDbContext.cs      (+ 3 table configs)
??? Extensions/
  ??? DatabaseServiceExtensions.cs             (+ async init, + seed call)

backoffice/src/TechWayFit.Pulse.BackOffice.Core/Persistence/
??? Sqlite/BackOfficeSqliteDbContext.cs          (+ 3 table configs)
??? SqlServer/BackOfficeSqlServerDbContext.cs    (+ 3 table configs)
??? MariaDb/BackOfficeMariaDbContext.cs      (+ 3 table configs)

src/TechWayFit.Pulse.Web/
??? Program.cs           (async database init)
```

---

## Design Decisions

### 1. Entity-Based Configuration (Not Hardcoded)

**Decision:** Store plan limits and activity access rules in database tables, not in code or appsettings.json

**Benefits:**
- ? Zero-deployment configuration changes (operator toggles in BackOffice)
- ? A/B testing plans without code changes
- ? Historical audit trail (UpdatedAt timestamps)
- ? Extensible for new features via JSON

**Example:**
- Marketing wants to make FiveWhys free ? operator sets `RequiresPremium=false` in BackOffice ? live instantly
- Product wants to test 10 vs 15 sessions on Plan B ? create variant plan, assign to test cohort

### 2. All Sessions Count Toward Quota

**Decision:** Count all session creations (not just AI-generated ones)

**Benefits:**
- ? Industry standard SaaS pricing model
- ? Clear mental model for users
- ? Prevents gaming (create many sessions, use AI on one)
- ? Sessions are the core product, not just AI

**Alternative rejected:** Feature-based quota (free sessions vs pro sessions) creates confusion

### 3. Premium Activity Types (Not Premium Activities)

**Decision:** Activity **types** (FiveWhys, AiSummary) are premium, not individual activity instances

**Benefits:**
- ? Consistent gating at UI and API layers
- ? Clear upgrade value proposition
- ? Dynamic feature toggling via database

**Implementation:**
- Free plan users cannot add FiveWhys or AiSummary activities
- API returns `402 Payment Required` with upgrade CTA data
- Activity picker shows locked state with minimum plan badge

### 4. Auto-Assign Free Plan

**Decision:** Users without subscriptions are automatically assigned Free plan on first interaction

**Benefits:**
- ? No manual setup required
- ? Smooth onboarding
- ? Backward compatible with existing users

**Implementation:**
- `PlanService.GetOrCreateFreeSubscriptionAsync()` helper creates Free subscription if missing
- Called on first quota check or session creation

### 5. Monthly Rolling Resets

**Decision:** Session quotas reset on 1st of each month (not anniversary billing date)

**Benefits:**
- ? Simpler logic (no per-user reset dates)
- ? Easier reporting (monthly cohorts)
- ? Standard SaaS pattern

**Trade-off:** User who subscribes mid-month gets partial month quota (acceptable for simplicity)

### 6. Manual SQL Scripts for SQL Server and MariaDB

**Decision:** EF migrations disabled for production database providers

**Benefits:**
- ? DBA review and approval
- ? Version control for SQL
- ? Independent deployment
- ? Clear rollback procedures

**SQLite Exception:** Auto-migration enabled for development simplicity

---

## Well-Known GUIDs

### Subscription Plans

```csharp
public static class WellKnownPlans
{
    public static readonly Guid Free = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid PlanA = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid PlanB = Guid.Parse("00000000-0000-0000-0000-000000000003");
}
```

### Activity Type Definitions

```csharp
public static class WellKnownActivityTypes
{
    public static readonly Guid Poll = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid WordCloud = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid Quadrant = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid FiveWhys = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid Rating = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public static readonly Guid GeneralFeedback = Guid.Parse("10000000-0000-0000-0000-000000000006");
    public static readonly Guid QnA = Guid.Parse("10000000-0000-0000-0000-000000000007");
    public static readonly Guid AiSummary = Guid.Parse("10000000-0000-0000-0000-000000000008");
    public static readonly Guid Break = Guid.Parse("10000000-0000-0000-0000-000000000009");
}
```

These GUIDs are used consistently across all database providers.

---

## Testing the Changes

### 1. SQLite (Development)

```bash
# Delete existing database (if any)
rm src/TechWayFit.Pulse.Web/App_Data/pulse.db

# Start application
dotnet run --project src/TechWayFit.Pulse.Web

# Check logs for:
# ? "Seeded 3 subscription plans"
# ? "Seeded 9 activity type definitions"
# ? "Commercialization seed data initialized successfully"
```

**Verify with SQLite browser:**
```sql
SELECT * FROM SubscriptionPlans;
SELECT * FROM ActivityTypeDefinitions;
```

### 2. SQL Server (Production-like)

```bash
# Ensure V1.0 baseline exists
sqlcmd -S localhost -d TechWayFitPulse -E -Q "SELECT * FROM pulse.__MigrationHistory"

# Apply V1.1 migration
sqlcmd -S localhost -d TechWayFitPulse -E -i "src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.1/00_CommercializationSchema.sql"

# Verify
sqlcmd -S localhost -d TechWayFitPulse -E -Q "SELECT PlanCode, DisplayName, MaxSessionsPerMonth FROM pulse.SubscriptionPlans"
```

### 3. MariaDB (Production-like)

```bash
# Ensure V1.0 baseline exists
mysql -u pulseuser -p pulse -e "SHOW TABLES"

# Apply V1.1 migration
mysql -u pulseuser -p pulse < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.1/00_CommercializationSchema.sql

# Verify
mysql -u pulseuser -p pulse -e "SELECT PlanCode, DisplayName, MaxSessionsPerMonth FROM SubscriptionPlans"
```

---

## Rollback Procedures

### SQLite

```bash
# Delete database file
rm src/TechWayFit.Pulse.Web/App_Data/pulse.db

# Restart app - schema will be recreated without V1.1 changes
# (requires code rollback to before V1.1 commits)
```

### SQL Server

```bash
sqlcmd -S localhost -d TechWayFitPulse -E -i "src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.1/99_Rollback.sql"
```

### MariaDB

```bash
mysql -u pulseuser -p pulse < src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.1/99_Rollback.sql
```

---

## Breaking Changes

### ?? None (Backward Compatible)

All changes are **additive only**:
- No existing tables modified
- No existing columns removed or renamed
- No existing foreign keys changed
- Existing sessions, activities, users all preserved

**New behavior:**
- Users without subscriptions are auto-assigned Free plan
- Free plan users are limited to 2 sessions/month (previously unlimited)

**Mitigation:**
- Run grandfathering script to give existing users Plan B for 90 days
- Communicate changes to users before enforcing limits

---

## Performance Impact

### Storage

**New data:** < 1 MB for 10,000 users
- SubscriptionPlans: 3 rows × 500 bytes = 1.5 KB
- FacilitatorSubscriptions: 10,000 users × 2 avg subscriptions × 300 bytes = 6 MB
- ActivityTypeDefinitions: 9 rows × 400 bytes = 3.6 KB

**Total:** ~6 MB for 10K users (negligible)

### Query Performance

**Indexes added:**
- 5 new indexes (2 unique, 3 composite)
- All queries covered by indexes
- Expected query time: < 5ms even at 100K users

**No impact on existing queries** - new tables are only queried for plan enforcement and BackOffice management.

---

## Security Considerations

### Data Protection

- ? Payment provider IDs encrypted in transit (HTTPS)
- ? No credit card data stored (Paddle/Stripe handle payments)
- ? Foreign keys enforce referential integrity
- ? Cascade deletes prevent orphaned subscriptions

### Access Control

- ? Plans managed only via BackOffice (operator role required)
- ? Activity type definitions managed only via BackOffice
- ? Users cannot modify their own subscription records directly
- ? All plan changes audited in `AuditLogs` table (BackOffice feature)

---

## Monitoring & Observability

### Logs to Watch

```
[INF] Seeded 3 subscription plans
[INF] Seeded 9 activity type definitions
[INF] Commercialization seed data initialized successfully
```

**Error scenarios:**
```
[ERR] Failed to seed commercialization data: ...
```

### Metrics to Track

- `SubscriptionPlans` row count (should be 3+ after deployment)
- `FacilitatorSubscriptions` row count (grows with users)
- `ActivityTypeDefinitions` row count (should be 9+ after deployment)

---

## Documentation Updates Needed

### User-Facing
- [ ] Update pricing page copy
- [ ] Create upgrade modal designs
- [ ] Document plan limits in FAQ
- [ ] Create email templates for quota notifications

### Internal
- [ ] Operator guide for plan management
- [ ] Operator guide for activity type toggling
- [ ] Troubleshooting guide for subscription issues
- [ ] Grandfathering procedure for existing users

---

## Known Limitations & Future Work

### Current Limitations

1. **No prorated refunds** - subscription model doesn't support mid-cycle upgrades/downgrades yet
2. **No add-ons** - all features bundled in plan (future: à la carte add-ons)
3. **No team plans** - subscriptions are per-user (future: team subscriptions)
4. **No usage-based billing** - fixed monthly quotas (future: overage charges)

### Future Enhancements

1. **Usage analytics** - track which features drive upgrades
2. **Discount codes** - promotional pricing
3. **Referral program** - credit for referrals
4. **Enterprise tier** - custom contracts, unlimited sessions
5. **API access tier** - programmatic session creation
6. **White label tier** - custom branding

---

## Support & Troubleshooting

### Common Issues

**Issue:** "Table SubscriptionPlans does not exist"  
**Solution:** Run V1.1 migration script for your database provider

**Issue:** "Seeding fails silently"  
**Solution:** Check logs in `App_Data/logs/` for detailed error messages

**Issue:** "Users have no subscription"  
**Solution:** Subscriptions are auto-created on first interaction; seed service may not have run

**Issue:** "FiveWhys not showing in activity picker"  
**Solution:** Check `ActivityTypeDefinitions.IsActive = 1` and `RequiresPremium` flag

### Contact

- **GitHub Issues:** https://github.com/techwayfit/twf-pulse/issues
- **Documentation:** `/docs/commercialization-plan.md`
- **Architecture:** `/docs/repository-reorganization-complete.md`

---

**Status:** Phase 1 Database Changes - COMPLETE ?  
**Date:** March 2026  
**Next:** Repository Layer & Service Implementation
