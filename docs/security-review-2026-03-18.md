# TechWayFit Pulse — Security Review (Phase 3)

**Review Date:** 18 March 2026  
**Scope:** API endpoints, middleware pipeline, token handling, and runtime protections in `TechWayFit.Pulse.Web`

## Summary

Phase 3 security hardening is now materially improved:
- facilitator-facing mutation endpoints are authenticated (`[Authorize]`) and still require facilitator token checks where applicable
- participant join/submit and AI generation paths are rate-limited
- CSP and baseline security headers are applied globally
- participant tokens now use multi-layer storage (memory + distributed cache + DB fallback) for multi-node resiliency
- BackOffice header token comparison now uses constant-time comparison

## Controls Implemented

1. Endpoint authorization hardening
- `SessionsController`, `ActivitiesController`, and `SessionAiController` are authenticated by default.
- Public read access remains explicitly allowed only where needed (for example, `GET /api/sessions/{code}`).

2. API rate limiting
- Added policies:
  - `participant-join`
  - `participant-submit`
  - `ai-generation`
  - `api-write`
- Applied policies to participant join, response submit, AI generation, and write-heavy facilitator endpoints.

3. Security headers / CSP
- Added middleware setting:
  - `Content-Security-Policy`
  - `X-Content-Type-Options`
  - `X-Frame-Options`
  - `Referrer-Policy`
  - `Permissions-Policy`

4. Token handling
- Participant token store now supports distributed cache and DB fallback.
- BackOffice token header comparison switched to constant-time comparison.

5. Sensitive logging
- Removed OpenAI API key metadata logging (`ApiKey.Length`).

## Residual Risks and Follow-Ups

1. CSRF posture for cookie-authenticated API mutations
- API endpoints protected by cookie auth should adopt explicit antiforgery strategy (or move to bearer-only auth for API mutation endpoints).

2. BackOffice auth model
- BackOffice currently uses a shared static token header. Next step: signed JWT or rotating HMAC-based token with expiry.

3. Authorization policy granularity
- Add explicit named policies (facilitator ownership/claims checks) to reduce duplicated ownership checks in controllers.

4. CSP tightening
- Current CSP allows `'unsafe-inline'`/`'unsafe-eval'` for compatibility. Next step: nonce/hash-based CSP and removal of unsafe directives.

## Decision

Phase 3 security hardening objectives are met for this implementation round. Remaining items are follow-up hardening tasks, not blockers for current functionality.
