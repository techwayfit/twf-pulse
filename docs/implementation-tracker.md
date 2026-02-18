# TechWayFit Pulse - Implementation Tracker

> Created: 18 February 2026  
> Purpose: Track prioritized feature delivery and codebase improvements identified from current docs + source scan.

---

## 1) Tracking Model

### Status Legend
- `Not Started`
- `In Progress`
- `Blocked`
- `Done`

### Priority Legend
- `P0` = Must do first (security, correctness, or major UX gap)
- `P1` = High value, next iteration
- `P2` = Important but can follow after P0/P1

### Owners
- Add owner initials/name per item as work begins.

---

## 2) Current Priorities (Ranked)

## A. Feature Completion

| ID | Priority | Item | Why it matters | Scope | Status | Owner | Target |
|---|---|---|---|---|---|---|---|
| FEAT-01 | P1 | Quadrant facilitator dashboard (live view) | Quadrant input exists for participants, but facilitator live dashboard fallback says coming soon | Add `QuadrantDashboard.razor`, wire to live facilitator component, include aggregation rendering | Done |  | 18 Feb 2026 |
| FEAT-02 | P1 | Five Whys strategy: implement fully or hide from creation UI | Five Whys can be created but currently falls back to generic experience | Either (A) complete participant/facilitator/presentation flow, or (B) hide modal/button until ready | Done (Hidden) |  | 18 Feb 2026 |
| FEAT-03 | P2 | QnA activity end-to-end | Enum exists, no complete workflow | Domain model, participant UI, dashboard, voting/moderation logic, creation flow | Not Started |  |  |
| FEAT-04 | P2 | Quiz activity end-to-end | Enum exists, no complete workflow | Config/response models, participant flow, scoring, dashboard insights, creation flow | Not Started |  |  |

### Notes for Feature Work
- Start with FEAT-01 for fastest user-visible value and low-to-medium implementation risk.
- FEAT-02 should be decided early to avoid user confusion from partially supported activity types.

---

## B. Security, Reliability, and Correctness

| ID | Priority | Item | Why it matters | Status | Owner | Target |
|---|---|---|---|---|---|---|
| ENG-01 | P0 | Encrypt stored OpenAI API key | Sensitive credential currently stored without encryption | Not Started |  |  |
| ENG-02 | P0 | Enforce facilitator auth on live controls | Token check still optional in live page path | Done |  | 18 Feb 2026 |
| ENG-03 | P1 | Replace placeholder response count with actual API-backed count | Facilitator sees inaccurate activity response metrics | Done |  | 18 Feb 2026 |
| ENG-04 | P1 | Validate existing participant session with server before auto-rejoin | Prevent stale local storage auto-join issues | Done |  | 18 Feb 2026 |
| ENG-05 | P1 | Replace delete confirmation stub with real confirmation flow | Prevent accidental destructive actions | Done |  | 18 Feb 2026 |

---

## C. Codebase Health and Technical Debt

| ID | Priority | Item | Why it matters | Status | Owner | Target |
|---|---|---|---|---|---|---|
| TECH-01 | P1 | Resolve nullability warnings in token stores/view models/views | Reduces runtime risk and build noise | Done |  | 18 Feb 2026 |
| TECH-02 | P1 | Add focused tests for activity workflows and dashboard services | Current test coverage is minimal (2 test files) | Done |  | 18 Feb 2026 |
| TECH-03 | P2 | Sync outdated status docs with actual implementation | Docs currently drift from source reality | Not Started |  |  |
| TECH-04 | P2 | Remove/replace inline style usage in activity components where practical | Align with project styling standards | Not Started |  |  |

---

## 3) Suggested Execution Plan

### Phase 1 (Immediate)
- ENG-01, ENG-02, ENG-03
- FEAT-01

### Phase 2 (Next)
- FEAT-02 decision and implementation path
- ENG-04, ENG-05
- TECH-01

### Phase 3 (After stabilization)
- FEAT-03, FEAT-04
- TECH-02, TECH-03, TECH-04

---

## 4) Implementation Checklist

### P0/P1 - Current Sprint
- [ ] ENG-01 Encrypt API key at rest
- [x] ENG-02 Enforce facilitator auth in live page actions
- [x] ENG-03 Implement actual response count retrieval
- [x] FEAT-01 Deliver Quadrant live facilitator dashboard
- [x] FEAT-02 Decide Five Whys path (Implement vs Hide)
- [x] TECH-01 Fix nullability warnings in identified files

### Next Sprint
- [x] ENG-04 Add server verification for participant local session reuse
- [x] ENG-05 Implement real delete confirmation UX
- [x] TECH-02 Add tests for dashboard services and activity flow

### Backlog
- [ ] FEAT-03 QnA full implementation
- [ ] FEAT-04 Quiz full implementation
- [ ] TECH-03 Update docs to match actual feature state
- [ ] TECH-04 Clean remaining style-standard violations

---

## 5) Change Log

### 18 Feb 2026
- Added initial consolidated tracker from code + documentation review.
- Captured feature gaps, engineering improvements, and execution phases.

### 18 Feb 2026 (P1 implementation pass)
- Delivered Quadrant facilitator live dashboard and wired it into live activity view.
- Hid Five Whys creation path pending full end-to-end implementation.
- Implemented API-backed live response count for current activity.
- Added server/token validation for participant auto-rejoin flow.
- Replaced delete confirmation stub with real confirmation prompt.
- Fixed prioritized nullability warnings and added dashboard service tests.

### 18 Feb 2026 (ENG-02)
- Enforced facilitator authorization for live session page initialization and controls.
- Removed optional-token live-page behavior and require validated facilitator token.
- Hardened token service to verify ownership before returning cached session token.

---

## 6) Update Protocol

When closing an item:
1. Update item status to `Done`.
2. Add linked PR/commit reference in this doc.
3. Add date under Change Log.
4. If scope changed, update this document before starting the next item.
