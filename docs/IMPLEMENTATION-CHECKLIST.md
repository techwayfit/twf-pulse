# Commercialization Implementation Checklist

## Phase 1: Core Services & Repositories ? COMPLETE

- [x] Create `IPlanService` interface
- [x] Create `IActivityTypeDefinitionRepository` interface
- [x] Verify `PlanFeatures` value object exists
- [x] Implement `PlanService` with:
  - [x] Auto-assign Free plan to new users
  - [x] Monthly quota reset logic
  - [x] Session creation quota check
  - [x] Feature access checking
  - [x] Activity type access checking
- [x] Implement `ActivityTypeDefinitionRepository`
- [x] Register services in DI container
- [x] Create unit tests for `PlanService`
- [x] Verify build succeeds
- [x] Create documentation

**Status:** ? Build Successful | 7 Unit Tests Created | 0 Errors

---

## Phase 2: Session Creation Gating ? COMPLETE

### Backend Changes
- [x] Inject `IPlanService` into `SessionsController`
- [x] Add quota check before session creation
  - [x] Call `CanCreateSessionAsync()`
  - [x] Return HTTP 402 if quota exhausted
  - [x] Include upgrade details in error response
- [x] Add quota consumption after successful creation
  - [x] Call `ConsumeSessionAsync()`
  - [x] Log quota usage
- [x] Create `AccountController` with `/api/account/plan-status` endpoint

### Frontend Changes
- [x] Update `CreateSession.cshtml` with quota banner
- [x] Update `create-session-form.js`:
  - [x] Fetch quota status on page load
  - [x] Display sessions used/allowed
  - [x] Show warning when approaching limit
  - [x] Handle 402 error with upgrade modal
  - [x] Add helper methods (buildSessionData, showError, hideError, setupMobileAddButton)
- [x] Create upgrade modal component (`_UpgradeModal.cshtml`)
- [x] Include upgrade modal in `_Layout.cshtml`
- [x] Test quota enforcement end-to-end

**Status:** ? Implementation Complete | Ready for Manual Testing  
**Actual Time:** 1 day (most code was already in place, added missing helper methods)

---

## Phase 3: AI Feature Gating ? COMPLETE

### Backend Changes
- [x] Update `SessionsController.GenerateActivitiesForSession`:
  - [x] Add `CanUseFeatureAsync("aiAssist")` check
  - [x] Return HTTP 402 if locked
- [x] Update `SessionsController.GenerateAiSummary`:
  - [x] Add `CanUseFeatureAsync("aiSummary")` check
  - [x] Return HTTP 402 if locked

### Frontend Changes
- [x] Update `AddActivities.cshtml` with AI locked banner
- [x] Update `add-activities.js`:
  - [x] Check AI feature access on page load
  - [x] Disable "Generate" button if locked
  - [x] Show upgrade prompt
  - [x] Handle 402 error with contextual message
- [x] Update `_UpgradeModal.cshtml` with context-aware messaging

**Status:** ? Implementation Complete | Ready for Manual Testing  
**Actual Time:** ~1 hour

---

## Phase 4: Activity Type Access Control ? TODO

### Backend Changes
- [ ] Create `ActivityTypesController`:
  - [ ] `GET /api/activity-types` endpoint
  - [ ] Return metadata with `isLocked` flag per user
- [ ] Update `SessionsController.AddActivity`:
  - [ ] Add `CanUseActivityTypeAsync()` check
  - [ ] Return HTTP 402 if activity type locked
- [ ] Create `ActivityTypeMetadataResponse` DTO

### Frontend Changes
- [ ] Create `activity-types.js` (NEW):
  - [ ] Fetch activity types from API
  - [ ] Render available vs locked activities
  - [ ] Handle locked activity clicks with upgrade modal
- [ ] Update `add-activities.js`:
  - [ ] Replace hardcoded activity types with dynamic API call
  - [ ] Render locked activities with badge
  - [ ] Add CSS for locked state
- [ ] Handle 402 error when adding locked activity

**Estimated Time:** 2 days (8-12 hours)

---

## Phase 5: Frontend Polish ? TODO

### New Pages
- [ ] Create `/account/billing` page:
  - [ ] Display current plan
  - [ ] Show quota usage with progress bar
  - [ ] List upgrade options
  - [ ] Link to upgrade flow
- [ ] Create `/home/pricing` page:
  - [ ] Fetch plans from API
  - [ ] Render pricing cards
  - [ ] Feature comparison table
  - [ ] CTA buttons

### Shared Components
- [ ] Create `_UpgradeModal.cshtml` partial:
  - [ ] Reusable modal for all upgrade prompts
  - [ ] Dynamic messaging based on context
  - [ ] Plan comparison
  - [ ] CTA buttons
- [ ] Add to `_Layout.cshtml`

### JavaScript Enhancements
- [ ] Create `billing.js`:
  - [ ] Fetch plan status
  - [ ] Render quota progress
  - [ ] Load upgrade options
- [ ] Create `pricing.js`:
  - [ ] Fetch all plans
  - [ ] Render pricing cards
  - [ ] Handle CTA clicks
- [ ] Update `create-session.js` with quota warnings

### CSS/Styling
- [ ] Add locked activity styles (`.locked`, `.lock-badge`)
- [ ] Add quota warning styles
- [ ] Add pricing card styles
- [ ] Add upgrade modal styles

**Estimated Time:** 2-3 days (12-18 hours)

---

## Phase 6: Testing & Integration ? TODO

### Unit Tests
- [ ] Test `PlanService.CanCreateSessionAsync` with various scenarios
- [ ] Test `PlanService.ConsumeSessionAsync` increments correctly
- [ ] Test `PlanService` monthly reset logic
- [ ] Test `PlanFeatures.FromJson` parsing
- [ ] Test activity type access with different plan configurations

### Integration Tests
- [ ] Test session creation with quota enforcement
- [ ] Test AI generation with feature gates
- [ ] Test activity creation with type gates
- [ ] Test monthly quota reset end-to-end
- [ ] Test auto-assignment of Free plan

### E2E Tests
- [ ] Free user: Create 2 sessions ? 3rd blocked
- [ ] Free user: Try AI Assist ? blocked with upgrade prompt
- [ ] Free user: Try FiveWhys ? blocked with upgrade prompt
- [ ] Plan A user: Can use all features
- [ ] Quota resets on 1st of month automatically

### Performance Tests
- [ ] Load test session creation with quota checks
- [ ] Measure query performance
- [ ] Identify caching opportunities

### Security Tests
- [ ] Verify quota cannot be bypassed
- [ ] Verify feature checks cannot be bypassed
- [ ] Verify plan data cannot be tampered with
- [ ] Test unauthorized access scenarios

**Estimated Time:** 2 days (12-16 hours)

---

## Additional Tasks (Post-Phase 6)

### Promo Code Integration
- [ ] Integrate `IPromoCodeService` with `IPlanService`
- [ ] Add promo code redemption API endpoint
- [ ] Add promo code input to billing page
- [ ] Test promo code application

### Payment Integration
- [ ] Integrate with payment provider (Stripe/Paddle)
- [ ] Create webhook handler
- [ ] Update subscription on payment success
- [ ] Handle payment failures
- [ ] Handle subscription cancellation

### Analytics & Monitoring
- [ ] Add application insights
- [ ] Track quota usage metrics
- [ ] Track conversion from Free to paid
- [ ] Monitor failed payment attempts
- [ ] Create admin dashboard for revenue tracking

---

## Team Assignments (Recommended)

### Backend Developer
- Phase 1: ? Complete
- Phase 2: Session gating (API changes)
- Phase 3: AI feature gating (API changes)
- Phase 4: Activity type access (API + ActivityTypesController)
- Phase 6: Backend testing

### Frontend Developer
- Phase 2: Create session UI updates
- Phase 3: AI Create tab updates
- Phase 4: Activity picker dynamic loading
- Phase 5: Billing page + Pricing page + Upgrade modals
- Phase 6: Frontend E2E testing

### QA Engineer
- Phase 6: Full test plan execution
- Create automated test suite
- Performance testing
- Security testing

---

## Success Metrics

### Phase 1 ?
- [x] Services compile without errors
- [x] Unit tests pass
- [x] Documentation complete

### Phase 2 Target
- [ ] Session creation properly gated
- [ ] HTTP 402 returned when quota exhausted
- [ ] Quota consumption tracked in database
- [ ] UI shows quota status

### Phase 3 Target
- [ ] AI features properly gated
- [ ] HTTP 402 returned when feature locked
- [ ] UI shows locked state

### Phase 4 Target
- [ ] Activity types dynamically loaded from API
- [ ] Premium activities show as locked for Free users
- [ ] HTTP 402 returned when trying to add locked activity

### Phase 5 Target
- [ ] Billing page functional and styled
- [ ] Pricing page shows all plans
- [ ] Upgrade modals consistent across app

### Phase 6 Target
- [ ] All tests passing
- [ ] Performance acceptable (<100ms for quota checks)
- [ ] No security vulnerabilities
- [ ] Ready for production deployment

---

## Roll-Back Plan

If issues are discovered in production:

### Phase 2 Roll-Back
```csharp
// Remove quota checks from SessionsController
// Comment out CanCreateSessionAsync + ConsumeSessionAsync calls
// Deploy
```

### Phase 3 Roll-Back
```csharp
// Remove feature checks from AI endpoints
// Comment out CanUseFeatureAsync calls
// Deploy
```

### Phase 4 Roll-Back
```csharp
// Remove ActivityTypesController
// Revert to hardcoded activity types in JavaScript
// Remove CanUseActivityTypeAsync check from AddActivity
// Deploy
```

### Complete Roll-Back
```csharp
// Comment out IPlanService registration in Program.cs
builder.Services.AddScoped<IPlanService, PlanService>(); // Comment this line

// All plan checks will be bypassed (no DI resolution)
// App continues to function without commercialization
```

---

## ?? Additional Resources

- **Implementation Plan:** `docs/commercialization-plan.md`
- **Phase 1 Complete:** `docs/phase1-implementation-complete.md`
- **Phase 2-6 Guide:** `docs/phase2-6-implementation-guide.md`
- **BackOffice Plan:** `docs/08-backoffice-plan.md`

---

**Current Status:** Phase 1 Complete ? | Ready for Phase 2 ??
