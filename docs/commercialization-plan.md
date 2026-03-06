# Commercialization Plan — TechWayFit Pulse

## Overview

This document analyses the technical changes required to introduce a freemium and paid tier model into the existing codebase. The analysis is based on the current state of the application as of March 2026.

---

## Pricing Tiers

| Tier | Sessions / month | AI Assist (create) | 5 Whys | AI Summary | Price |
|---|---|---|---|---|---|
| **Free** | 2 | ✗ | ✗ | ✗ | $0 |
| **Plan A** | 5 | ✓ | ✓ | ✓ | $10/mo |
| **Plan B** | 15 | ✓ | ✓ | ✓ | $20/mo |

---

## Current State Analysis

### What Already Exists (Can Be Leveraged)

| Component | Location | Relevance |
|---|---|---|
| `AiQuotaService` | `Application/Services/AiQuotaService.cs` | Monthly quota pattern (used/reset/check) — directly reusable for session counting |
| `FacilitatorUserData` KV store | `Domain/Entities/FacilitatorUserData.cs` | Plan data can be stored here using new keys, same as AI quota keys |
| `FacilitatorUserDataKeys` | Same file | Add `Plan.Type`, `Plan.SessionsUsed`, `Plan.SessionsResetDate` here |
| `IAiQuotaService` / `AiQuotaOptions` | `Application/Abstractions/Services/IAiQuotaService.cs` | Pattern to copy for session quota |
| Sessions linked to `FacilitatorUserId` | `Domain/Entities/Session.cs` | Counting is straightforward via existing session repository |
| BackOffice | `/backoffice/src/...` | Operator portal exists; add plan management there |
| `FiveWhys` activity type | UI hidden, enum defined (`ActivityType.FiveWhys = 6`) | Already blocked from UI — just needs plan-based gate when re-enabled |
| `AiSummary` activity type | Fully implemented | Needs plan gate added |
| AI Assist (`SessionAIService`) | `AI/Services/SessionAIService.cs` | Already quota-checked — quota logic to be replaced/extended by plan service |

### What Does NOT Exist Yet

- No concept of a subscription plan on `FacilitatorUser`
- No session creation quota (only AI generation quota exists)
- No payment integration
- No pricing/upgrade pages
- No plan-based feature gating

---

## Required Changes

### 1. Domain Layer — `TechWayFit.Pulse.Domain`

**New File: `Domain/Entities/SubscriptionPlan.cs`**

Define plans as first-class entities stored in the database:

```csharp
namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Defines a subscription plan with quota limits and feature access.
/// System-defined, operator-managed via BackOffice. Not created by facilitators.
/// </summary>
public sealed class SubscriptionPlan
{
    public Guid Id { get; }
    public string PlanCode { get; private set; }        // 'free', 'plan-a', 'plan-b'
    public string DisplayName { get; private set; }  // 'Free', 'Plan A', 'Plan B'
    public string? Description { get; private set; }
    public decimal PriceMonthly { get; private set; }
    public decimal? PriceYearly { get; private set; }
    public int MaxSessionsPerMonth { get; private set; }
    public string FeaturesJson { get; private set; }    // JSON: {"aiAssist": true, "fiveWhys": true, ...}
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
```

**New File: `Domain/Entities/FacilitatorSubscription.cs`**

Track current and historical subscriptions:

```csharp
namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Tracks a facilitator's subscription to a plan.
/// One active record per user, historical records retained for audit.
/// </summary>
public sealed class FacilitatorSubscription
{
    public Guid Id { get; }
    public Guid FacilitatorUserId { get; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }  // Active, Canceled, Expired, Trial
    public DateTimeOffset StartsAt { get; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }
    
    // Usage tracking (resets monthly)
    public int SessionsUsed { get; private set; }
    public DateTimeOffset SessionsResetAt { get; private set; }
    
    // Payment integration
    public string? PaymentProvider { get; private set; }        // 'paddle', 'stripe', null (operator-assigned)
    public string? ExternalCustomerId { get; private set; }     // Paddle customer ID
    public string? ExternalSubscriptionId { get; private set; } // Paddle subscription ID
    
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
}

public enum SubscriptionStatus
{
    Active = 0,
    Canceled = 1,
    Expired = 2,
    Trial = 3
}
```

**New File: `Domain/ValueObjects/PlanFeatures.cs`**

Type-safe feature flags parsed from JSON:

```csharp
namespace TechWayFit.Pulse.Domain.ValueObjects;

/// <summary>
/// Feature flags for a subscription plan, parsed from JSON column.
/// Extensible — new features can be added to JSON without schema changes.
/// </summary>
public sealed record PlanFeatures(
    bool AiAssist,
bool FiveWhys,
    bool AiSummary)
{
    /// <summary>Parse from JSON string stored in database</summary>
    public static PlanFeatures FromJson(string json)
    {
        var doc = System.Text.Json.JsonDocument.Parse(json);
        return new PlanFeatures(
    doc.RootElement.TryGetProperty("aiAssist", out var ai) && ai.GetBoolean(),
            doc.RootElement.TryGetProperty("fiveWhys", out var fw) && fw.GetBoolean(),
            doc.RootElement.TryGetProperty("aiSummary", out var sum) && sum.GetBoolean());
    }

  /// <summary>Serialize to JSON string for database storage</summary>
    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            aiAssist = AiAssist,
            fiveWhys = FiveWhys,
        aiSummary = AiSummary
  });
    }
    
    /// <summary>Check if a specific feature is enabled</summary>
public bool HasFeature(string featureName)
    {
        return featureName.ToLowerInvariant() switch
        {
    "aiassist" => AiAssist,
     "fivewhys" => FiveWhys,
     "aisummary" => AiSummary,
    _ => false
      };
    }
}
```

**Migration Required**: New tables `SubscriptionPlans` and `FacilitatorSubscriptions`.

**Seed Data**: Free, Plan A, and Plan B pre-populated on application first run or via migration script.

---

### 2. Application Layer — `TechWayFit.Pulse.Application`

#### 2a. New Repository Interfaces

**New file: `Application/Abstractions/Repositories/ISubscriptionPlanRepository.cs`**

```csharp
public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SubscriptionPlan?> GetByCodeAsync(string planCode, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionPlan>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default);
    Task UpdateAsync(SubscriptionPlan plan, CancellationToken ct = default);
}
```

**New file: `Application/Abstractions/Repositories/IFacilitatorSubscriptionRepository.cs`**

```csharp
public interface IFacilitatorSubscriptionRepository
{
    Task<FacilitatorSubscription?> GetActiveSubscriptionAsync(Guid facilitatorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<FacilitatorSubscription>> GetSubscriptionHistoryAsync(Guid facilitatorUserId, CancellationToken ct = default);
    Task<FacilitatorSubscription?> GetByExternalSubscriptionIdAsync(string externalSubscriptionId, CancellationToken ct = default);
    Task AddAsync(FacilitatorSubscription subscription, CancellationToken ct = default);
    Task UpdateAsync(FacilitatorSubscription subscription, CancellationToken ct = default);
}
```

#### 2b. New service interface

**New file: `Application/Abstractions/Services/IPlanService.cs`**

```csharp
public interface IPlanService
{
    // Plan queries (read-only - plans managed via BackOffice)
    Task<IReadOnlyList<SubscriptionPlan>> GetAvailablePlansAsync(CancellationToken ct = default);
    Task<SubscriptionPlan?> GetPlanByCodeAsync(string planCode, CancellationToken ct = default);
    
    // User subscription queries
    Task<PlanStatus> GetPlanStatusAsync(Guid facilitatorUserId, CancellationToken ct = default);
    Task<FacilitatorSubscription?> GetActiveSubscriptionAsync(Guid facilitatorUserId, CancellationToken ct = default);
    
    // Quota checks
    Task<bool> CanCreateSessionAsync(Guid facilitatorUserId, CancellationToken ct = default);
    Task ConsumeSessionAsync(Guid facilitatorUserId, CancellationToken ct = default);
    
    // Feature gates
    Task<bool> CanUseFeatureAsync(Guid facilitatorUserId, PremiumFeature feature, CancellationToken ct = default);
    
    // Subscription management (called by payment webhooks or BackOffice operators)
    Task<FacilitatorSubscription> AssignPlanAsync(
        Guid facilitatorUserId, 
   string planCode, 
 SubscriptionStatus status,
        DateTimeOffset startsAt,
        DateTimeOffset? expiresAt,
     string? paymentProvider = null,
        string? externalCustomerId = null,
        string? externalSubscriptionId = null,
CancellationToken ct = default);
    
    Task<FacilitatorSubscription> UpgradePlanAsync(Guid facilitatorUserId, string newPlanCode, CancellationToken ct = default);
    Task CancelSubscriptionAsync(Guid facilitatorUserId, CancellationToken ct = default);
}

public enum PremiumFeature 
{ 
    AiAssist = 0, 
    FiveWhys = 1, 
    AiSummary = 2 
}

public sealed record PlanStatus(
    string PlanCode,// 'free', 'plan-a', 'plan-b'
    string PlanDisplayName,    // 'Free', 'Plan A', 'Plan B'
    int SessionsUsed,
    int SessionsAllowed,
    DateTimeOffset? ResetAt,
    DateTimeOffset? ExpiresAt,
    SubscriptionStatus Status,
    PlanFeatures Features);    // Feature flags for this plan
```

#### 2c. Implement `PlanService`

**New file: `Application/Services/PlanService.cs`**

Follow the same monthly reset pattern as `AiQuotaService`, but read plan limits from database entities:

```csharp
public sealed class PlanService : IPlanService
{
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IFacilitatorSubscriptionRepository _subscriptionRepo;
    private readonly ILogger<PlanService> _logger;

    public PlanService(
        ISubscriptionPlanRepository planRepo,
        IFacilitatorSubscriptionRepository subscriptionRepo,
        ILogger<PlanService> logger)
    {
        _planRepo = planRepo;
    _subscriptionRepo = subscriptionRepo;
      _logger = logger;
    }

    public async Task<PlanStatus> GetPlanStatusAsync(Guid facilitatorUserId, CancellationToken ct = default)
    {
 var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, ct);
        await ResetQuotaIfNeededAsync(subscription, ct);
        
        var plan = await _planRepo.GetByIdAsync(subscription.PlanId, ct)
            ?? throw new InvalidOperationException($"Plan {subscription.PlanId} not found");

        var features = PlanFeatures.FromJson(plan.FeaturesJson);

        return new PlanStatus(
      plan.PlanCode,
      plan.DisplayName,
          subscription.SessionsUsed,
          plan.MaxSessionsPerMonth,
       subscription.SessionsResetAt,
            subscription.ExpiresAt,
            subscription.Status,
    features);
    }

    public async Task<bool> CanCreateSessionAsync(Guid facilitatorUserId, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, ct);
        await ResetQuotaIfNeededAsync(subscription, ct);
        
 var plan = await _planRepo.GetByIdAsync(subscription.PlanId, ct);
        if (plan == null) return false;

        return subscription.Status == SubscriptionStatus.Active 
     && subscription.SessionsUsed < plan.MaxSessionsPerMonth;
    }

    public async Task ConsumeSessionAsync(Guid facilitatorUserId, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, ct);
        await ResetQuotaIfNeededAsync(subscription, ct);
        
     subscription.ConsumeSession(DateTimeOffset.UtcNow);
      await _subscriptionRepo.UpdateAsync(subscription, ct);
    }

  public async Task<bool> CanUseFeatureAsync(Guid facilitatorUserId, PremiumFeature feature, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, ct);
        if (subscription.Status != SubscriptionStatus.Active) return false;

        var plan = await _planRepo.GetByIdAsync(subscription.PlanId, ct);
        if (plan == null) return false;

 var features = PlanFeatures.FromJson(plan.FeaturesJson);
        
        return feature switch
        {
            PremiumFeature.AiAssist => features.AiAssist,
            PremiumFeature.FiveWhys => features.FiveWhys,
        PremiumFeature.AiSummary => features.AiSummary,
            _ => false
        };
    }

  // ... AssignPlanAsync, UpgradePlanAsync, CancelSubscriptionAsync implementations
    // ... GetOrCreateFreeSubscriptionAsync helper (auto-assigns free plan to new users)
  // ... ResetQuotaIfNeededAsync helper (monthly reset logic)
}
```

Key characteristics:
- Plans read from database via `ISubscriptionPlanRepository`
- Plan limits and features are dynamic, not hardcoded
- Monthly reset logic same as `AiQuotaService` pattern
- Auto-assigns free plan to new users on first quota check

Register in `Program.cs`:
```csharp
// No PlanOptions needed — plans live in database
builder.Services.AddScoped<IPlanService, PlanService>();
```

---

### 3. Session Creation Gate — `TechWayFit.Pulse.Web`

**File: `Controllers/Api/SessionsController.cs`** (POST `/api/sessions`)

Before persisting a new session, call `IPlanService.CanCreateSessionAsync`. Return `HTTP 402 Payment Required` or `HTTP 403 Forbidden` with structured error:

```csharp
[HttpPost]
public async Task<ActionResult<ApiResponse<CreateSessionResponse>>> CreateSession(
    [FromBody] CreateSessionRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var facilitatorUserId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        
        // Gate: Require authentication for session creation
        if (!facilitatorUserId.HasValue)
        {
        return Unauthorized(Error<CreateSessionResponse>(
      "authentication_required", 
   "Sign in to create sessions"));
        }
 
 // Gate: Check session creation quota
    var canCreate = await _planService.CanCreateSessionAsync(facilitatorUserId.Value, cancellationToken);
    if (!canCreate)
    {
var status = await _planService.GetPlanStatusAsync(facilitatorUserId.Value, cancellationToken);
        return StatusCode(402, Error<CreateSessionResponse>(
        "session_limit_reached",
$"You have used {status.SessionsUsed} of {status.SessionsAllowed} sessions this month. " +
      $"Upgrade to Plan A ({_planAPlanSessions} sessions) or Plan B ({_planBPlanSessions} sessions)."));
    }

    // Create session (existing logic)
    var session = await _sessions.CreateSessionAsync(
        code, request.Title, request.Goal, request.Context,
        settings, joinFormSchema, DateTimeOffset.UtcNow,
      facilitatorUserId, request.GroupId, cancellationToken);
    
    // Consume quota after successful creation
    await _planService.ConsumeSessionAsync(facilitatorUserId.Value, cancellationToken);
    
    _logger.LogInformation(
        "Session {Code} created and quota consumed for user {UserId} - {Used}/{Allowed} used",
        session.Code, facilitatorUserId.Value, 
        status.SessionsUsed + 1, status.SessionsAllowed);
    
    return Ok(Wrap(new CreateSessionResponse(session.Id, session.Code)));
}
```

Error response format:
```json
{
  "data": null,
  "errors": [{
    "code": "session_limit_reached",
    "message": "You have used 2 of 2 sessions this month. Upgrade to Plan A for 5 sessions.",
    "details": {
 "sessionsUsed": 2,
      "sessionsAllowed": 2,
      "resetDate": "2026-04-01T00:00:00Z",
      "planCode": "free",
   "upgradePlans": [
        { "code": "plan-a", "sessions": 5, "price": 10.00 },
  { "code": "plan-b", "sessions": 15, "price": 20.00 }
      ]
    }
  }]
}
```

This provides all the data the UI needs to render the upgrade modal dynamically.

---

## Alternative Considered: Feature-Based Quota

**Not recommended** because it creates a confusing two-tier system:

- "Free sessions" (no AI, no premium activities) → unlimited?
- "Pro sessions" (with AI/FiveWhys/AiSummary) → counts toward quota?

This splits your value proposition and makes pricing unclear. Users will ask:
- "Can I create unlimited free sessions?"
- "What's the point of upgrading if I don't use AI?"
- "Do I need to mark each session as Pro or not?"

**Sessions ARE the product** — not just AI features. Counting all sessions is the cleanest approach.

---

## Summary: Hardcoded vs Database-Driven Configuration

### Comparison Table

| Aspect | Hardcoded Approach (Current) | Database-Driven Approach (Recommended) |
|--------|------------------------------|----------------------------------------|
| **Premium activities** | `if (type == FiveWhys \|\| type == AiSummary)` in code | Query `ActivityTypeDefinitions.RequiresPremium` |
| **Adding "Quiz" as premium** | Change code, redeploy app | Update 1 row in BackOffice UI |
| **Making FiveWhys free (promo)** | Change code, redeploy app | Set `RequiresPremium=0` in BackOffice |
| **A/B testing pricing** | Not possible without deployment | Create `plan-a-variant`, assign to test users |
| **Activity display order** | Hardcoded in `getAvailableTypes()` array | Update `SortOrder` column via BackOffice |
| **Activity icons/colors** | Hardcoded in JavaScript | Edit `IconClass` and `ColorHex` in database |
| **Hiding unfinished features** | Comment out code or feature flag | Set `IsActive=0` in database |
| **Plan feature matrix** | Hardcoded in `PlanOptions` config | Query `SubscriptionPlans.FeaturesJson` |
| **Plan limits** | `PlanOptions.FreeSessionsPerMonth = 2` | `SubscriptionPlans.MaxSessionsPerMonth` |
| **Operator autonomy** | Requires engineering for any change | Full self-service via BackOffice |
| **Historical audit** | No record of plan/feature changes | Full `UpdatedAt` timestamp trail |

### Architecture Benefits

**Zero-Deployment Configuration Changes:**
- Marketing decides to make FiveWhys free → operator flips toggle in BackOffice → live instantly
- Product wants to test 10 vs 15 sessions on Plan B → create new plan row → assign to test cohort
- Support needs to comp a user → assign custom plan via BackOffice → no engineering ticket

**Extensibility:**
- Add "Enterprise" tier with custom session limits → `INSERT INTO SubscriptionPlans`
- Add new activity type "Survey" → add enum value + seed row in `ActivityTypeDefinitions`
- Add new premium feature "Custom Branding" → add to `FeaturesJson`: `{"customBranding": true}`

**Consistency:**
- Single source of truth for plan limits, features, and activity access
- UI automatically reflects current configuration (no stale hardcoded values)
- API validation uses same data source as UI display

---

## Performance & Caching Strategy

### Challenge: Database Lookups on Every Request

With entity-based configuration, every session creation and activity addition requires database queries:
- `CanCreateSessionAsync` → reads `FacilitatorSubscription` + `SubscriptionPlan`
- `CanUseActivityTypeAsync` → reads `FacilitatorSubscription` + `SubscriptionPlan` + `ActivityTypeDefinition`
- `GET /api/activity-types` → reads all `ActivityTypeDefinitions`

For a high-traffic endpoint like `POST /api/sessions/activities`, this could become a bottleneck.

### Solution: Multi-Layer Caching

#### Layer 1: In-Memory Cache for Read-Heavy Data

**Plans and Activity Type Definitions are read-heavy:**
- Plans change rarely (monthly at most)
- Activity type definitions change even less frequently (when new features launch)

**Implementation:**
```csharp
// In PlanService or new CachedPlanService decorator
public class CachedPlanService : IPlanService
{
  private readonly IPlanService _inner;
    private readonly IMemoryCache _cache;
    private const string PLAN_CACHE_KEY_PREFIX = "plan:";
    private const string ACTIVITY_TYPE_CACHE_KEY_PREFIX = "activitytype:";
    private static readonly TimeSpan PLAN_CACHE_DURATION = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ACTIVITY_TYPE_CACHE_DURATION = TimeSpan.FromHours(1);

    public async Task<SubscriptionPlan?> GetPlanByCodeAsync(string planCode, CancellationToken ct = default)
    {
 var cacheKey = $"{PLAN_CACHE_KEY_PREFIX}{planCode}";
   return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PLAN_CACHE_DURATION;
       return await _inner.GetPlanByCodeAsync(planCode, ct);
        });
    }

    public async Task<IReadOnlyList<ActivityTypeDefinition>> GetAllActivityTypesAsync(CancellationToken ct = default)
    {
    const string cacheKey = "activitytypes:all:active";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
 {
      entry.AbsoluteExpirationRelativeToNow = ACTIVITY_TYPE_CACHE_DURATION;
 return await _activityTypeDefRepo.GetAllActiveAsync(ct);
        });
  }
    
    // Invalidate cache when operator updates configuration
    public async Task InvalidatePlanCacheAsync(string planCode)
    {
   _cache.Remove($"{PLAN_CACHE_KEY_PREFIX}{planCode}");
   _cache.Remove("plans:all:active"); // If you cache GetAllActiveAsync
    }

    public async Task InvalidateActivityTypeCacheAsync()
    {
  _cache.Remove("activitytypes:all:active");
    }
}
```

#### Layer 2: Request-Scoped Caching for User Subscriptions

**Subscriptions are user-specific and read multiple times per request:**
- Session creation checks quota → reads subscription
- Activity addition checks activity type access → reads same subscription again

**Implementation:**
```csharp
// In PlanService
private readonly Dictionary<Guid, FacilitatorSubscription> _requestCache = new();

private async Task<FacilitatorSubscription> GetOrCreateFreeSubscriptionAsync(
    Guid facilitatorUserId, 
    CancellationToken ct)
{
    // Check request-scoped cache first
    if (_requestCache.TryGetValue(facilitatorUserId, out var cached))
    {
   return cached;
    }

    // Load from database
    var subscription = await _subscriptionRepo.GetActiveSubscriptionAsync(facilitatorUserId, ct);
    if (subscription == null)
    {
 // Auto-assign free plan
     var freePlan = await GetPlanByCodeAsync("free", ct)
        ?? throw new InvalidOperationException("Free plan not found");
        
        subscription = new FacilitatorSubscription(
  Guid.NewGuid(), facilitatorUserId, freePlan.Id, 
 SubscriptionStatus.Active, DateTimeOffset.UtcNow, null,
      0, DateTimeOffset.UtcNow.AddMonths(1), 
    null, null, null,
 DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        
        await _subscriptionRepo.AddAsync(subscription, ct);
    }

    // Cache for this request
    _requestCache[facilitatorUserId] = subscription;
  return subscription;
}
```

**Caveat:** `PlanService` must be registered as **Scoped** (not Singleton) for request-scoped cache to work safely.

#### Layer 3: CDN Caching for Public Endpoints

**Endpoint: `GET /api/plans` (unauthenticated pricing page)**

Add cache headers:
```csharp
[HttpGet]
[ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept")]
public async Task<ActionResult<ApiResponse<IReadOnlyList<PlanResponse>>>> GetPlans(...)
{
    var plans = await _planService.GetAvailablePlansAsync(cancellationToken);
    // ... return plans ...
}
```

**Endpoint: `GET /api/activity-types` (authenticated but cacheable per user)**

```csharp
[HttpGet]
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, VaryByHeader = "Cookie")]
public async Task<ActionResult<ApiResponse<IReadOnlyList<ActivityTypeMetadataResponse>>>> GetActivityTypes(...)
{
    // Cache for 5 minutes per user (client-side)
}
```

### Performance Impact Analysis

| Operation | Without Caching | With Caching | Improvement |
|-----------|-----------------|--------------|-------------|
| **Session creation** | 3 DB queries (subscription, plan, consume) | 1 DB query (consume only) | ~67% reduction |
| **Activity addition** | 4 DB queries (subscription, plan, activity def, create) | 1 DB query (create only) | ~75% reduction |
| **Load activity picker** | 10 DB queries (1 per activity type) | 0 DB queries (cached) | 100% reduction |
| **Load pricing page** | 3 DB queries (3 plans) | 0 DB queries (CDN cached) | 100% reduction |

**Trade-off:** 
- Eventual consistency — plan/activity config changes take up to 15 minutes to propagate
- Acceptable for this use case (configuration changes are rare operational events)
- Can add manual cache invalidation button in BackOffice for immediate effect

---

## Complete Architecture Flow

### Session Creation with Plan Enforcement

```
┌─────────────────────────────────────────────────────────────────────┐
│ User: Create Session (POST /api/sessions) │
└────────────────────────┬────────────────────────────────────────────┘
          │
           ▼
         ┌───────────────────────────────┐
         │ SessionsController        │
         │ ┌───────────────────────────┐ │
 │ │ 1. Authenticate user      │ │
         │ │ 2. Check session quota ───┼─┼───► PlanService.CanCreateSessionAsync()
         │ │ 3. Create session entity  │ │
   │ │ 4. Consume quota ─────────┼─┼──────────┤
│ └───────────────────────────┘ │          │
  └───────────────────────────────┘       │
      ▼
  ┌────────────────────────────────────┐
      │ PlanService          │
      │ ┌────────────────────────────────┐ │
         │ │ Read FacilitatorSubscription   │ │◄─┐
      │ │ Read SubscriptionPlan          │ │  │
            │ │ Check SessionsUsed < MaxLimit  │ │  │ Memory
   │ │ Increment SessionsUsed    │ │  │ Cache
 │ │ Monthly reset if needed        │ │  │ (15min TTL)
  │ └────────────────────────────────┘ │  │
    └───────────┬────────────────────────┘  │
            │    │
         ▼    │
          ┌──────────────────────────────────┐         │
    │ Database (SQLite)          │   │
         │ ┌──────────────────────────────┐ │         │
          │ │ SubscriptionPlans            │ │─────────┘
│ │ - free (2 sessions)     │ │
          │ │ - plan-a (5 sessions)        │ │
    │ │ - plan-b (15 sessions)       │ │
         │ └──────────────────────────────┘ │
         │ ┌──────────────────────────────┐ │
        │ │ FacilitatorSubscriptions     │ │
      │ │ - UserId → PlanId      │ │
        │ │ - SessionsUsed = 1 (updated) │ │
         │ │ - SessionsResetAt = Apr 1    │ │
   │ └──────────────────────────────┘ │
   └──────────────────────────────────┘
```

### Activity Type Access with Plan Enforcement

```
┌─────────────────────────────────────────────────────────────────────┐
│ User: Add FiveWhys Activity (POST /api/sessions/{code}/activities) │
└────────────────────────┬────────────────────────────────────────────┘
       │
            ▼
   ┌───────────────────────────────┐
       │ SessionsController    │
         │ ┌───────────────────────────┐ │
      │ │ 1. Authenticate user      │ │
    │ │ 2. Validate session owner │ │
  │ │ 3. Check activity access ─┼─┼───► PlanService.CanUseActivityTypeAsync(FiveWhys)
         │ │ 4. Create activity entity │ │  │
         │ └───────────────────────────┘ │          │
         └───────────────────────────────┘      │
          ▼
       ┌────────────────────────────────────┐
     │ PlanService        │
  │ ┌────────────────────────────────┐ │
              │ │ Read FacilitatorSubscription   │ │
   │ │ Read SubscriptionPlan     │ │
   │ │ Read ActivityTypeDefinition ───┼─┼──┐
    │ │ Check RequiresPremium flag     │ │  │
      │ │ Compare plan vs MinPlanCode│ │  │
       │ └────────────────────────────────┘ │  │
         └───────────┬────────────────────────┘  │
    │ │
          ▼    │
    ┌──────────────────────────────────┐         │
     │ Database (SQLite)         │         │
 │ ┌──────────────────────────────┐ │   │
           │ │ ActivityTypeDefinitions      │ │◄────────┘
            │ │ - FiveWhys:         │ │  Memory Cache
      │ │   RequiresPremium = true     │ │  (1hr TTL)
   │ │   MinPlanCode = 'plan-a'     │ │
           │ │   IsActive = true            │ │
       │ └──────────────────────────────┘ │
   │ ┌──────────────────────────────┐ │
       │ │ FacilitatorSubscriptions     │ │
        │ │ - User's PlanId = Free       │ │
        │ └──────────────────────────────┘ │
                │ ┌──────────────────────────────┐ │
          │ │ SubscriptionPlans         │ │
       │ │ - Free: aiAssist = false     │ │
         │ └──────────────────────────────┘ │
     └──────────────────────────────────┘
        │
        ▼
    Result: ❌ Access Denied → HTTP 402
   "Five Whys requires Plan A or above"
```

### Frontend Activity Picker Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│ User: Opens Add Activities Page              │
└────────────────────────┬───────────────────────────────────────────────┘
             │
  ▼
    ┌───────────────────────────────┐
         │ JavaScript (activity-types.js)│
 │ ┌───────────────────────────┐ │
 │ │ Fetch /api/activity-types │ │
         │ └───────────┬───────────────┘ │
         └─────────────┼─────────────────┘
              │
   ▼
  ┌─────────────────────────────┐
         │ ActivityTypesController       │
         │ ┌───────────────────────────┐ │
    │ │ Get all active types      │ │
    │ │ For each type: │ │
         │ │   - Load definition       │ │◄─────┐
         │ │   - Check user's plan │ │      │ Memory Cache
      │ │   - Set isLocked flag     │ │      │ (1hr TTL)
         │ └───────────────────────────┘      │
  └─────────────┼─────────────────┘      │
           │            │
   ▼       │
      ┌─────────────────────────────┐        │
      │ Database   │      │
         │ ActivityTypeDefinitions     │────────┘
         └─────────────┬───────────────┘
    │
         ▼
         ┌─────────────────────────────┐
  │ API Response                │
         │ [    │
         │   { type: "Poll",         │
         │     isLocked: false },      │
         │   { type: "FiveWhys",       │
         │     isLocked: true,  │
      │     minPlanCode: "plan-a" } │
         │ ]          │
         └─────────────┬───────────────┘
             │
   ▼
 ┌─────────────────────────────┐
         │ UI Rendering    │
         │ ┌─────────────────────────┐ │
         │ │ Free activities:        │ │
         │ │   [Poll] [WordCloud]    │ │
         │ │ Premium (locked):       │ │
         │ │   [🔒 FiveWhys]         │ │
         │ │   [🔒 AI Summary]       │ │
   │ └─────────────────────────┘ │
         └─────────────────────────────┘
```

---

## Risk & Considerations

| Area | Risk | Mitigation |
|---|---|---|
| Existing users | All current users default to Free (2 sessions/month) which may break active users | Grandfather existing users to Plan B for 90 days via migration script |
| AI quota vs plan quota | Two separate quota systems could conflict | Deprecate `AiQuotaService` — fold AI gating into `PlanService` feature checks |
| BYOK users | Currently bypass AI quota entirely; should paid plan users also get platform AI? | Plan A/B includes platform AI; BYOK remains as an alternative, not a workaround. Free plan cannot use BYOK. |
| Session counting | A session created then immediately deleted still consumes quota | By design — counts creation, not completion. Prevents abuse. |
| 5 Whys maturity | Activity type is deferred/incomplete per docs | Complete implementation before exposing as a premium feature |
| Paddle dependency | Payment processor coupling | Keep all Paddle logic behind `IBillingService` interface for testability and future provider swaps |
| VAT / tax compliance | Selling internationally without Merchant of Record exposes TechWayFit to tax obligations | Paddle acts as MoR — they collect and remit all taxes globally by default |
| Plan schema changes | Adding new features requires code changes to `PlanFeatures` record | Features stored as JSON — new properties can be added and checked dynamically via `HasFeature(string)` method |
| Database seeding | Plans must exist before app can run | Migration includes seed data for Free/Plan A/Plan B; free plan created with well-known GUID |
| Concurrent plan updates | Two admins editing same plan simultaneously | Use optimistic concurrency (UpdatedAt timestamp check) or pessimistic locking |
| Cache invalidation | Plan/activity config changes not reflected immediately | Implement cache invalidation API or use short TTL (5-15 min) with eventual consistency |
| Stale activity metadata | User sees unlocked activity in UI, but API rejects (cache mismatch) | Use same cache key in both `GET /api/activity-types` and `AddActivity` validation; ensure consistent TTL |
| Multi-instance deployment | Memory cache doesn't sync across web servers | Use distributed cache (Redis) or accept 15-minute eventual consistency |
| Request-scoped cache memory | Large request cache could increase memory pressure | Limit to small objects (subscriptions, not full activity lists); scoped services are disposed per request |

---

This migration should run **once** before Phase 1 is released to production, or as part of the Phase 1 deployment script.

---

## Complete Database Schema

### New Tables Summary

The commercialization system requires **three new tables**:

#### 1. `SubscriptionPlans` — System Plan Definitions

| Column | Type | Description |
|--------|------|-------------|
| `Id` | GUID | Primary key |
| `PlanCode` | VARCHAR(50) | Unique identifier ('free', 'plan-a', 'plan-b') |
| `DisplayName` | VARCHAR(100) | User-facing name ('Free', 'Plan A', 'Plan B') |
| `Description` | VARCHAR(500) | Marketing description |
| `PriceMonthly` | DECIMAL(10,2) | Monthly price in USD |
| `PriceYearly` | DECIMAL(10,2) | Annual price (optional, for discount) |
| `MaxSessionsPerMonth` | INT | Session creation quota |
| `FeaturesJson` | NVARCHAR(MAX) | JSON: `{"aiAssist": true, "fiveWhys": true, ...}` |
| `IsActive` | BIT | Show in pricing page? |
| `SortOrder` | INT | Display order |
| `CreatedAt` | DATETIMEOFFSET | Creation timestamp |
| `UpdatedAt` | DATETIMEOFFSET | Last modified timestamp |

**Seed Data:** 3 rows (Free, Plan A, Plan B)

#### 2. `FacilitatorSubscriptions` — User Subscription State

| Column | Type | Description |
|--------|------|-------------|
| `Id` | GUID | Primary key |
| `FacilitatorUserId` | GUID | FK to `FacilitatorUsers` |
| `PlanId` | GUID | FK to `SubscriptionPlans` |
| `Status` | VARCHAR(20) | Active, Canceled, Expired, Trial |
| `StartsAt` | DATETIMEOFFSET | Subscription start date |
| `ExpiresAt` | DATETIMEOFFSET | Expiry date (null = monthly rolling) |
| `CanceledAt` | DATETIMEOFFSET | Cancellation date |
| `SessionsUsed` | INT | Current month's session count |
| `SessionsResetAt` | DATETIMEOFFSET | Next reset date (monthly) |
| `PaymentProvider` | VARCHAR(50) | 'paddle', 'stripe', null (operator-assigned) |
| `ExternalCustomerId` | VARCHAR(200) | Paddle/Stripe customer ID |
| `ExternalSubscriptionId` | VARCHAR(200) | Paddle/Stripe subscription ID |
| `CreatedAt` | DATETIMEOFFSET | Creation timestamp |
| `UpdatedAt` | DATETIMEOFFSET | Last modified timestamp |

**Indexes:**
- `IX_FacilitatorSubscriptions_UserId_Status` on `(FacilitatorUserId, Status)` — fast active subscription lookup
- `IX_FacilitatorSubscriptions_ExternalSubscriptionId` on `(ExternalSubscriptionId)` — webhook reconciliation

**Seed Data:** None initially — users auto-assigned Free plan on first interaction

#### 3. `ActivityTypeDefinitions` — Activity Metadata & Access Rules

| Column | Type | Description |
|--------|------|-------------|
| `Id` | GUID | Primary key |
| `ActivityType` | INT | FK to `ActivityType` enum (0-10) |
| `DisplayName` | VARCHAR(100) | User-facing name ('5 Whys Analysis') |
| `Description` | VARCHAR(500) | Feature description |
| `IconClass` | VARCHAR(100) | CSS class ('ics ics-question ic-sm') |
| `ColorHex` | VARCHAR(7) | Badge color (#F59E0B) |
| `RequiresPremium` | BIT | Locked for Free plan? |
| `MinPlanCode` | VARCHAR(50) | Minimum plan required (null = free) |
| `IsActive` | BIT | Show in UI? |
| `SortOrder` | INT | Display order in activity picker |
| `CreatedAt` | DATETIMEOFFSET | Creation timestamp |
| `UpdatedAt` | DATETIMEOFFSET | Last modified timestamp |

**Indexes:**
- `UNIQUE` constraint on `ActivityType` (one row per enum value)
- `IX_ActivityTypeDefinitions_IsActive_SortOrder` on `(IsActive, SortOrder)` — fast active list query

**Seed Data:** 9 rows (one per implemented activity type: Poll, WordCloud, Quadrant, FiveWhys, Rating, Feedback, QnA, AiSummary, Break)

### Entity Relationships

```
FacilitatorUsers ─┬─< FacilitatorSubscriptions >─┬─ SubscriptionPlans
       │         │
      └─< Sessions          └─ ActivityTypeDefinitions
      └─< Activities     (linked via enum, not FK)
 └─< Responses
```

**Key Relationships:**
- **1 User → N Subscriptions** (historical trail)
- **1 User → 1 Active Subscription** (current plan)
- **1 Plan → N Subscriptions** (many users on same plan)
- **1 ActivityType → 1 Definition** (metadata for each activity type)

### Storage Estimates

| Table | Rows (Year 1) | Size per Row | Total Size |
|-------|---------------|--------------|------------|
| `SubscriptionPlans` | ~5 | ~500 bytes | 2.5 KB |
| `FacilitatorSubscriptions` | ~1,000 users × 2 avg subscriptions | ~300 bytes | 600 KB |
| `ActivityTypeDefinitions` | ~15 (all activity types) | ~400 bytes | 6 KB |

**Total additional storage:** < 1 MB

**SQLite performance:** All tables have proper indexes; expected query time < 5ms even at 10K users.

---

## Summary

The **entity-based plan architecture** provides:
- ✅ Zero-code plan additions (operator-managed via BackOffice)
- ✅ Fast, indexed queries for reporting and enforcement
- ✅ Full audit trail of subscription changes
- ✅ Dynamic feature flags via JSON
- ✅ Natural integration with payment webhooks
- ✅ Future-proof for enterprise tiers, add-ons, and multi-tenancy

The **entity-based activity type configuration** provides:
- ✅ Zero-code premium activity changes (operator toggle in BackOffice)
- ✅ Dynamic UI rendering based on database configuration
- ✅ A/B testing which features are premium vs free
- ✅ Consistent gating at API and UI layers
- ✅ Extensible for new activity types without hardcoding

This approach requires **three new database tables** but eliminates technical debt from hardcoding plan logic in application code. It aligns with the existing entity-driven architecture (Sessions, Activities, Groups, etc.) and sets up the application for long-term commercialization success.

---

## Quick Reference: Key Decision Summary

### ✅ Decisions Made

| Decision | Rationale |
|----------|-----------|
| **Count all sessions at creation** | Industry standard, prevents gaming, clear mental model |
| **Require authentication for session creation** | Enables plan enforcement and commercialization |
| **Database-driven plan configuration** | Operator autonomy, zero-deployment changes, extensibility |
| **Database-driven activity type configuration** | Eliminate hardcoding, dynamic premium feature toggling |
| **Deprecate `AiQuotaService`** | Fold AI gating into plan feature checks, unified quota system |
| **Free plan cannot use BYOK** | AI is a premium feature, not a loophole to bypass pricing |
| **Deleted sessions don't refund quota** | Prevent create/delete gaming, count creation events |
| **Activity types cached 1hr, plans cached 15min** | Balance performance vs configuration freshness |
| **Paddle as payment processor** | Merchant of Record for global tax compliance |

### 📋 Implementation Checklist (Phase 1)

**Domain Layer:**
- [ ] Create `SubscriptionPlan` entity
- [ ] Create `FacilitatorSubscription` entity
- [ ] Create `ActivityTypeDefinition` entity
- [ ] Create `PlanFeatures` value object
- [ ] Add `SubscriptionStatus` enum

**Infrastructure Layer:**
- [ ] Create migration for `SubscriptionPlans` table
- [ ] Create migration for `FacilitatorSubscriptions` table
- [ ] Create migration for `ActivityTypeDefinitions` table
- [ ] Add seed data for 3 plans (Free, Plan A, Plan B)
- [ ] Add seed data for 9 activity types
- [ ] Implement `ISubscriptionPlanRepository`
- [ ] Implement `IFacilitatorSubscriptionRepository`
- [ ] Implement `IActivityTypeDefinitionRepository`

**Application Layer:**
- [ ] Create `IPlanService` interface
- [ ] Implement `PlanService` with quota and feature checking
- [ ] Add `CanUseActivityTypeAsync` method
- [ ] Implement caching decorator `CachedPlanService`

**Web API:**
- [ ] Create `GET /api/plans` endpoint
- [ ] Create `GET /api/account/plan-status` endpoint
- [ ] Create `GET /api/activity-types` endpoint
- [ ] Gate `POST /api/sessions` (session creation quota)
- [ ] Gate `POST /api/sessions/{code}/generate-activities` (AI Assist feature)
- [ ] Gate `POST /api/sessions/{code}/activities` (activity type access)

**Web UI:**
- [ ] Update `activity-types.js` to fetch from API
- [ ] Add session quota warning banner on Create Session page
- [ ] Add confirmation modal for last session
- [ ] Add quota exceeded modal with upgrade CTAs
- [ ] Update Add Activities page to render locked activities dynamically
- [ ] Add "Upgrade Required" modal
- [ ] Update Profile page with Plan & Billing section

**BackOffice:**
- [ ] Create Plan Management page (CRUD for `SubscriptionPlans`)
- [ ] Create Activity Type Management page (edit `ActivityTypeDefinitions`)
- [ ] Add Plan column to user list
- [ ] Add manual plan assignment in user detail page
- [ ] Add revenue report (users per plan, session usage)

**Configuration:**
- [ ] Register `IPlanService` in `Program.cs`
- [ ] Register repositories in `Program.cs`
- [ ] Add `IMemoryCache` for plan/activity type caching
- [ ] Remove or deprecate `AiQuotaService` registration

---

## Next Steps

1. **Review and approve this plan** with stakeholders (product, marketing, engineering)
2. **Prioritize Phase 1 features** — determine which can be deferred
3. **Create migration scripts** with proper seed data and rollback procedures
4. **Set up BackOffice local environment** for testing plan management
5. **Define grandfathering policy** for existing users (recommend Plan B for 90 days)
6. **Write end-to-end tests** for quota enforcement and feature gating
7. **Document operator procedures** for manual plan assignment and troubleshooting
8. **Plan Paddle account setup** (sandbox for Phase 2 testing)
9. **Design email templates** for quota notifications (Phase 3)
10. **Create pricing page copy** and marketing assets

**Estimated Engineering Effort:**
- Phase 1 (Plan enforcement): **2-3 weeks** (1 backend engineer, 1 frontend engineer)
- Phase 2 (Paddle integration): **1-2 weeks** (webhook testing is time-intensive)
- Phase 3 (Polish): **1 week** (can be incremental post-launch)

**Total: 4-6 weeks** to production-ready freemium launch.

---

## Appendix: Complete Data Flow Example

### Scenario: Free User Creates Session with AI and Tries to Add FiveWhys

```
┌─────────────────────────────────────────────────────────────────────────┐
│ User Action 1: Click "Create Session" │
└─────────────────────────┬───────────────────────────────────────────────┘
               │
                     ▼
         ┌─────────────────────────┐
           │ POST /api/sessions      │
  └─────────┬───────────────┘
       │
       ▼
    ┌─────────────────────────────────┐
│ PlanService.CanCreateSessionAsync │
            └─────────┬───────────────────────┘
            │
 ▼
           ┌────────────────────────────────────────┐
        │ Database Query:     │
       │ SELECT * FROM FacilitatorSubscriptions │
           │ WHERE FacilitatorUserId = @userId      │
  │   AND Status = 'Active'       │
      └─────────┬──────────────────────────────┘
        │
         ▼
      Result: Free plan (2 sessions/month)
     SessionsUsed = 1, SessionsAllowed = 2
  │
       ▼
     ✅ Quota check passed (1 < 2)
           │
       ▼
    ┌─────────────────────────┐
      │ Create Session entity   │
         └─────────┬───────────────┘
                 │
        ▼
    ┌────────────────────────────────────┐
    │ PlanService.ConsumeSessionAsync    │
    └─────────┬──────────────────────────┘
  │
            ▼
       ┌────────────────────────────────────────┐
       │ Database Update:     │
  │ UPDATE FacilitatorSubscriptions        │
  │ SET SessionsUsed = 2 │
           └────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ User Action 2: Click "AI Assist" to generate activities     │
└─────────────────────────┬───────────────────────────────────────────────┘
       │
  ▼
       ┌─────────────────────────────────────────┐
  │ POST /api/sessions/{code}/generate-activities │
   └─────────┬───────────────────────────────────┘
    │
      ▼
          ┌─────────────────────────────────┐
 │ PlanService.GetPlanStatusAsync  │
          └─────────┬───────────────────────┘
    │
        ▼
           ┌────────────────────────────────────────┐
      │ Database Queries:     │
   │ 1. Read FacilitatorSubscription        │
 │ 2. Read SubscriptionPlan │
└─────────┬──────────────────────────────┘
           │
     ▼
    Result: Free plan
                  Features = {"aiAssist": false, "fiveWhys": false, "aiSummary": false}
                │
          ▼
    ❌ Feature check failed (aiAssist = false)
   │
        ▼
            ┌─────────────────────────────────────────┐
       │ HTTP 402 Payment Required      │
    │ "AI Assist is not available on the Free │
         │  plan. Upgrade to Plan A."     │
        └─────────┬───────────────────────────────┘
        │
  ▼
          ┌─────────────────────────┐
  │ Frontend: Show upgrade  │
          │ modal with Plan A/B CTAs│
    └─────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ User Action 3: Manually add FiveWhys activity  │
└─────────────────────────┬───────────────────────────────────────────────┘
          │
           ▼
       ┌─────────────────────────────────────────┐
     │ POST /api/sessions/{code}/activities      │
        │ { type: "FiveWhys", title: "..." }         │
  └─────────┬─────────────────────────────────┘
     │
       ▼
     ┌──────────────────────────────────────────────┐
            │ PlanService.CanUseActivityTypeAsync(FiveWhys) │
            └─────────┬────────────────────────────────────┘
      │
   ▼
        ┌────────────────────────────────────────┐
     │ Database Queries:       │
           │ 1. Read FacilitatorSubscription      │
           │ 2. Read SubscriptionPlan │
   │ 3. Read ActivityTypeDefinition │
  │    WHERE ActivityType = 6 (FiveWhys)   │
     └─────────┬──────────────────────────────┘
  │
          ▼
   Result: FiveWhys definition
      RequiresPremium = true
           MinPlanCode = 'plan-a'
        │
     User's plan = 'free'
          │
        ▼
 ❌ Access check failed (free < plan-a)
   │
             ▼
     ┌─────────────────────────────────────────┐
            │ HTTP 402 Payment Required           │
         │ "Five Whys is only available on Plan A  │
    │  and above."       │
              └─────────┬───────────────────────────────┘
          │
           ▼
          ┌─────────────────────────┐
          │ Frontend: Show upgrade  │
       │ modal with Plan A/B CTAs│
       └─────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ User Action 4: Upgrade to Plan A via Paddle   │
└─────────────────────────┬───────────────────────────────────────────────┘
           │
          ▼
  ┌─────────────────────────┐
     │ Paddle Checkout Overlay │
   │ (client-side, Paddle.js)│
       └─────────┬───────────────┘
         │ (payment processed by Paddle)
       ▼
       ┌─────────────────────────────────────────┐
       │ Paddle Webhook: subscription.created   │
       │ POST /api/paddle/webhook        │
   └─────────┬───────────────────────────────┘
     │
     ▼
   ┌────────────────────────────────────────┐
         │ PaddleWebhookController      │
         │ 1. Verify signature       │
       │ 2. Parse passthrough data      │
         │    (facilitatorUserId, planCode)  │
    │ 3. Call PlanService.AssignPlanAsync    │
      └─────────┬──────────────────────────────┘
             │
          ▼
         ┌────────────────────────────────────────┐
    │ Database Updates:      │
   │ 1. Expire old Free subscription        │
│    (Status = 'Expired')  │
│ 2. Create new Plan A subscription      │
    │    (Status = 'Active')       │
           │ 3. Store Paddle customer/subscription  │
           │    IDs in FacilitatorSubscription      │
           └────────────────────────────────────────┘
       │
        ▼
           ✅ User now has Plan A access
              SessionsAllowed = 5
              Features = {"aiAssist": true, "fiveWhys": true, "aiSummary": true}

┌─────────────────────────────────────────────────────────────────────────┐
│ User Action 5: Retry AI Assist  │
└─────────────────────────┬───────────────────────────────────────────────┘
              │
      ▼
         ┌─────────────────────────────────────────┐
      │ POST /api/sessions/{code}/generate-activities │
  └─────────┬───────────────────────────────────┘
            │
       ▼
            ┌─────────────────────────────────┐
    │ PlanService: Check plan.Features.AiAssist │
            └─────────┬───────────────────────┘
          │
         ▼
 Result: Plan A, aiAssist = true
       │
      ▼
    ✅ Feature check passed
 │
       ▼
              ┌─────────────────────────┐
        │ SessionAIService        │
    │ Generate 6 activities   │
    └─────────┬───────────────┘
   │
         ▼
              ┌─────────────────────────┐
      │ Return AI-generated     │
              │ agenda to client     │
    └─────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ User Action 6: Add FiveWhys activity   │
└─────────────────────────┬───────────────────────────────────────────────┘
    │
  ▼
         ┌─────────────────────────────────────────┐
        │ POST /api/sessions/{code}/activities      │
            │ { type: "FiveWhys" }          │
    └─────────┬─────────────────────────────────┘
  │
       ▼
       ┌────────────────────────────────────────────┐
    │ PlanService.CanUseActivityTypeAsync(FiveWhys) │
         └─────────┬──────────────────────────────────┘
  │
      ▼
         Result: ActivityTypeDefinition (FiveWhys)
        RequiresPremium = true, MinPlanCode = 'plan-a'
       User's plan = 'plan-a'
      │
   ▼
     ✅ Access check passed (plan-a >= plan-a)
         │
  ▼
       ┌─────────────────────────┐
  │ Create FiveWhys activity│
       │ entity in database      │
  └─────────┬───────────────┘
        │
       ▼
         ┌─────────────────────────┐
       │ SignalR: Broadcast      │
              │ activity added event    │
└─────────────────────────┘
```

### Key Takeaways

**For Engineers:**
- Three new tables, all with proper indexes and seed data
- Existing `Session` and `Activity` entities **unchanged**
- Caching layer prevents performance regression
- Clean separation: plans control quotas, activity types control feature access

**For Operators:**
- Full control over pricing tiers without code deployment
- Can toggle premium activity flags in BackOffice UI
- Manual plan assignment for support/comps
- Revenue and usage analytics out of the box

**For Users:**
- Clear, predictable pricing: sessions + features bundled per plan
- Immediate feedback when hitting limits (with upgrade CTAs)
- No hidden restrictions or confusing toggles
- Standard SaaS billing experience via Paddle
