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

**File: `Domain/Entities/FacilitatorUserData.cs`**

Add new well-known keys to `FacilitatorUserDataKeys`:

```csharp
// Plan / Subscription
public const string PlanType            = "Plan.Type";           // "Free" | "PlanA" | "PlanB"
public const string PlanExpiresAt       = "Plan.ExpiresAt";      // ISO DateTimeOffset, null = monthly rolling
public const string PlanSessionsUsed    = "Plan.SessionsUsed";   // int, resets monthly
public const string PlanSessionsResetAt = "Plan.SessionsResetAt"; // ISO DateTimeOffset
```

No migration required — these are stored in the existing `FacilitatorUserData` table.

---

### 2. Application Layer — `TechWayFit.Pulse.Application`

#### 2a. New options class

**New file: `Application/Services/PlanOptions.cs`**

```csharp
public class PlanOptions
{
    public const string SectionName = "Plans";
    public int FreeSessionsPerMonth  { get; set; } = 2;
    public int PlanASessionsPerMonth { get; set; } = 5;
    public int PlanBSessionsPerMonth { get; set; } = 15;
}
```

Register in `appsettings.json`:
```json
"Plans": {
  "FreeSessionsPerMonth": 2,
  "PlanASessionsPerMonth": 5,
  "PlanBSessionsPerMonth": 15
}
```

#### 2b. New service interface

**New file: `Application/Abstractions/Services/IPlanService.cs`**

```csharp
public interface IPlanService
{
    Task<PlanStatus> GetPlanStatusAsync(Guid facilitatorUserId, CancellationToken ct = default);
    Task<bool> CanCreateSessionAsync(Guid facilitatorUserId, CancellationToken ct = default);
    Task ConsumeSessionAsync(Guid facilitatorUserId, CancellationToken ct = default);
    Task<bool> CanUseFeatureAsync(Guid facilitatorUserId, PremiumFeature feature, CancellationToken ct = default);
    Task AssignPlanAsync(Guid facilitatorUserId, string planType, DateTimeOffset? expiresAt, CancellationToken ct = default);
}

public enum PremiumFeature { AiAssist, FiveWhys, AiSummary }

public sealed record PlanStatus(
    string PlanType,      // "Free" | "PlanA" | "PlanB"
    int SessionsUsed,
    int SessionsAllowed,
    DateTimeOffset? ResetAt,
    DateTimeOffset? ExpiresAt,
    bool IsActive);
```

#### 2c. Implement `PlanService`

**New file: `Application/Services/PlanService.cs`**

Follow the exact same pattern as `AiQuotaService`:
- Read/write `FacilitatorUserData` keys
- Monthly reset logic (same as `ResetQuotaIfNeededAsync`)
- `CanCreateSessionAsync` returns `false` if `SessionsUsed >= SessionsAllowed`
- `CanUseFeatureAsync` returns `false` for `Free` plan on all `PremiumFeature` values
- Premium features are available to `PlanA` and `PlanB` only

Register in `Program.cs`:
```csharp
builder.Services.Configure<PlanOptions>(builder.Configuration.GetSection(PlanOptions.SectionName));
builder.Services.AddScoped<IPlanService, PlanService>();
```

---

### 3. Session Creation Gate — `TechWayFit.Pulse.Web`

**File: `Controllers/FacilitatorController.cs`** (CreateSession POST action)

Before persisting a new session, call `IPlanService.CanCreateSessionAsync`. Return a view error if denied.

**File: `Controllers/Api/SessionsController.cs`** (POST `/api/sessions`)

Same check before creating the session entity. Return `HTTP 402` or `HTTP 403` with a structured error:

```json
{
  "error": {
    "code": "session_limit_reached",
    "message": "You have used 2 of 2 sessions this month. Upgrade to Plan A for 5 sessions."
  }
}
```

After successful session creation, call `IPlanService.ConsumeSessionAsync`.

---

### 4. Feature Gating

#### 4a. AI Assist during session creation

**File: `Controllers/Api/SessionsController.cs`** — `POST /{code}/generate-activities`  
**File: `AI/Services/SessionAIService.cs`**

Current code checks AI quota via `IAiQuotaService`. Replace / extend this check:
- On **Free** plan → block AI generation regardless of BYOK
- On **Plan A / Plan B** → allow (can still use BYOK or platform key)

The existing `AiQuotaService` can be deprecated in its current form once plan-level AI gating is introduced. The BYOK path can remain for paid plan users.

#### 4b. 5 Whys activity

**File: `wwwroot/js/activity-types.js`** — `ActivityFactory.getAvailableTypes()`  
**File: `Views/Shared/_ActivityFormModals.cshtml`**  
**File: `Controllers/Api/SessionsController.cs`** — activity creation endpoint

- UI: When the facilitator adds activities, render the `FiveWhys` card with a lock badge and tooltip "Available on Plan A and above" for Free users. Fetch plan status via a small API endpoint on page load.
- API: Validate that the facilitator's plan allows `FiveWhys` before persisting the activity.

#### 4c. AI Summary activity

Same approach as 5 Whys above — lock in UI and validate at the API level.

---

### 5. New API Endpoint

**File: `Controllers/Api/SessionsController.cs`** (or a new `AccountApiController`)

```
GET /api/account/plan-status
```

Returns `PlanStatus` JSON. Used by:
- Session creation page to show session quota remaining
- Activity builder page to show locked activity types
- Profile page

---

### 6. UI Changes

#### 6a. Profile page

**File: `Views/Account/Profile.cshtml`**

Add a "Plan & Billing" section alongside the existing AI quota section:
- Current plan badge (`Free` / `Plan A` / `Plan B`)
- Sessions used / allowed this month (progress bar — same pattern as AI quota)
- Upgrade CTA buttons for Free users
- Plan expiry date if applicable

#### 6b. Session creation page

**File: `Views/Facilitator/CreateSession.cshtml`**

- Show a banner: *"You have X of Y sessions remaining this month"*
- Disable the AI Assist button and show tooltip for Free-plan users

#### 6c. Activity builder

**File: `Views/Facilitator/AddActivities.cshtml`** (or equivalent)

- Show `FiveWhys` and `AiSummary` tiles with a lock icon and upgrade tooltip for Free users
- On click, open an upsell modal instead of the activity config modal

#### 6d. New pricing page

**New file: `Views/Home/Pricing.cshtml`** (static MVC page)

Comparison table with Free / Plan A / Plan B. Link to `/account/upgrade`.

#### 6e. Upgrade flow

**New controller: `Controllers/BillingController.cs`**

Routes:
- `GET /billing/upgrade` — plan selection page
- `POST /billing/checkout` — redirect to Paddle-hosted Checkout overlay
- `GET /billing/success` — confirmation page (Paddle redirects here after payment)
- `GET /billing/cancel` — cancellation page

---

### 7. Payment Integration (Paddle)

Paddle is the recommended payment processor. It acts as **Merchant of Record**, meaning Paddle handles VAT, GST, and sales tax globally — removing that compliance burden from TechWayFit entirely. This is the primary reason to prefer it over Stripe for an internationally-sold SaaS product.

#### 7a. Paddle SDK

No official .NET NuGet package exists. Integration is done via:
- **Paddle.js** (frontend overlay checkout — a `<script>` tag, no complex frontend build needed)
- **Paddle REST API** (server-side subscription management, called via `HttpClient`)
- **Paddle Webhooks** (server-side event handling)

Create a lightweight `PaddleApiClient` wrapper using `IHttpClientFactory` — no third-party NuGet required.

#### 7b. Paddle configuration

```json
"Paddle": {
  "VendorId": "12345",
  "ApiKey": "...",
  "WebhookPublicKey": "...",
  "PlanAMonthlyPriceId": "pri_...",
  "PlanBMonthlyPriceId": "pri_...",
  "Environment": "sandbox"
}
```

**New file: `Web/Options/PaddleOptions.cs`** — strongly-typed config class.

#### 7c. `BillingController.cs`

- `GET /billing/upgrade` — renders the pricing page with Paddle.js loaded; clicking a plan triggers the Paddle overlay checkout directly in the browser (no server round-trip needed for checkout initiation)
- `POST /billing/webhook` — receives Paddle webhook events (see 7d)
- Include `facilitatorUserId` as a Paddle **custom data** field (`passthrough`) when initialising the overlay, so webhooks can reconcile the purchase back to the user

#### 7d. Paddle Webhook controller

**New file: `Controllers/Api/PaddleWebhookController.cs`**

Verify the webhook signature using Paddle's public key before processing. Handle these events:

| Paddle Event | Action |
|---|---|
| `subscription.created` | Call `IPlanService.AssignPlanAsync`, store `PaddleCustomerId` + `PaddleSubscriptionId` |
| `subscription.updated` | Update plan type (e.g., upgrade/downgrade between Plan A and B) |
| `subscription.canceled` | Downgrade to Free at period end |
| `transaction.completed` (one-off) | If you later offer top-up packs |

#### 7e. Store Paddle customer ID

Add to `FacilitatorUserDataKeys`:
```csharp
public const string PaddleCustomerId     = "Paddle.CustomerId";
public const string PaddleSubscriptionId = "Paddle.SubscriptionId";
```

---

### 8. BackOffice Changes

**Repo: `/backoffice/src/TechWayFit.Pulse.BackOffice`**

Operator portal needs:
- New **Plans** column in the user list view (`backoffice-template/users.html`)
- New **User Detail** section showing plan type, sessions used, expiry
- Manual plan override: ability to assign a plan to any user from the operator UI (for support/comps)
- Revenue report: count of users per plan, monthly session usage totals

Due to the BackOffice being a separate codebase, it will need its own API calls to the main app (or direct DB access following existing pattern).

---

### 9. Database / Migrations

No new tables are required if plan data is stored in `FacilitatorUserData` (same pattern as AI quota). However, a migration will be needed if:

- You want to add a `PlanType` column directly to `FacilitatorUsers` for query performance (e.g., operator filtering, batch reporting)
- You add a `Stripe.CustomerId` column for direct indexed lookups

**Recommended migration**: Add `PlanType NVARCHAR(20) NULL DEFAULT 'Free'` and `PaddleCustomerId NVARCHAR(100) NULL` to `FacilitatorUsers`.

This allows efficient operator queries like *"show all Plan B users"* without scanning `FacilitatorUserData`.

---

## Implementation Phases

### Phase 1 — Plan enforcement (no payment, operator-assigned)

1. Add `FacilitatorUserDataKeys` plan keys
2. Implement `PlanOptions`, `IPlanService`, `PlanService`
3. Gate session creation in `FacilitatorController` and `SessionsController`
4. Gate AI Assist, FiveWhys, AiSummary at API and UI
5. Add `GET /api/account/plan-status` endpoint
6. Update Profile page with plan status
7. BackOffice: manual plan assignment
8. Register `PlanOptions` and `PlanService` in `Program.cs`

> At this stage, plans are assigned manually by an operator. The app enforces limits. No payment flow yet.

### Phase 2 — Self-service upgrade (Paddle)

1. Add `PaddleOptions` configuration and `PaddleApiClient` HTTP wrapper
2. Add Paddle.js script to `_Layout.cshtml` (single `<script>` tag)
3. Implement `BillingController` (upgrade page, webhook endpoint)
4. Implement `PaddleWebhookController` with signature verification
5. Add pricing page (`Views/Home/Pricing.cshtml`) with Paddle overlay triggers
6. Add upgrade CTA in profile and session creation pages
7. Store Paddle IDs in `FacilitatorUserData`

### Phase 3 — Polish

1. Email notifications on plan upgrade / expiry / session limit reached
2. Grace period logic (e.g., allow 1 extra session after limit with a warning)
3. Annual billing option

---

## Risk & Considerations

| Area | Risk | Mitigation |
|---|---|---|
| Existing users | All current users default to Free (2 sessions/month) which may break active users | Grandfather existing users to Plan B for 90 days |
| AI quota vs plan quota | Two separate quota systems could conflict | Deprecate `AiQuotaService` and fold AI gating into `PlanService` |
| BYOK users | Currently bypass AI quota entirely; should Paid plan users also get platform AI? | Plan A/B includes platform AI; BYOK remains as an alternative, not a workaround |
| Session counting | A session created then immediately deleted still consumes quota | By design — counts creation, not completion |
| 5 Whys maturity | Activity type is deferred/incomplete per docs | Complete implementation before exposing as a premium feature |
| Paddle dependency | Payment processor coupling | Keep all Paddle logic behind `IBillingService` interface for testability and future provider swaps |
| VAT / tax compliance | Selling internationally without Merchant of Record exposes TechWayFit to tax obligations | Paddle acts as MoR — they collect and remit all taxes globally by default |
