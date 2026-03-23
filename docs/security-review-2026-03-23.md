# Security Architecture Review — TechWayFit Pulse

**Date:** 23 March 2026  
**Reviewer:** Security Architect (GitHub Copilot)  
**Scope:** Full codebase review — Web, Application, Infrastructure, AI layers  
**Framework:** OWASP Top 10 (2021)

---

## Executive Summary

The application has a solid security foundation: cookie auth uses `HttpOnly` + `SecurePolicy = Always`, OTP codes are generated with `RandomNumberGenerator`, Data Protection encrypts API keys, CSRF tokens are validated on all POST forms, EF Core is used exclusively (no raw SQL), and rate limiting is configured on key endpoints.

However, **5 critical** and **8 high** severity findings were identified. The most impactful are an unauthenticated AI controller, missing access control on the SignalR hub subscription, a Server-Side Request Forgery (SSRF) vector via user-controlled AI base URLs, and missing brute-force protection on OTP verification.

---

## Findings

### CRITICAL

---

#### SEC-001 — `AiController` Missing `[Authorize]`

| Field | Detail |
|---|---|
| **Severity** | Critical |
| **OWASP** | A01 — Broken Access Control |
| **File** | `src/TechWayFit.Pulse.Web/Controllers/AiController.cs` |

The entire `/api/ai` controller — including `GET /api/ai/participant/analyze`, `GET /api/ai/facilitator/prompt`, and `POST /api/ai/five-whys/next` — is accessible by any unauthenticated caller. Any internet user can invoke AI features and consume quota/credits without a session or identity.

```csharp
[ApiController]
[Route("api/ai")]
// ← MISSING [Authorize]
public class AiController : ControllerBase
```

**Recommendation:** Add `[Authorize]` at the class level. If participant-facing AI endpoints are needed, add `RequireParticipantToken` validation matching the pattern in `ResponsesController`.

---

#### SEC-002 — `DashboardsController` Exposes Participant Data Without Authentication

| Field | Detail |
|---|---|
| **Severity** | Critical |
| **OWASP** | A01 — Broken Access Control |
| **File** | `src/TechWayFit.Pulse.Web/Controllers/Api/DashboardsController.cs` |

`GET /{code}/participants/{participantId}/dashboard` and `GET /{code}/dashboards` have no `[Authorize]` attribute and no participant or facilitator token check. Any caller who knows a session code and a participant GUID can retrieve all response and dashboard data for that participant.

**Recommendation:** Add `[Authorize]` to the controller, or add `RequireParticipantToken` / `RequireFacilitatorToken` at the action level where anonymous participant access is intentional.

---

#### SEC-003 — `GetParticipantResponses` Missing Token Validation

| Field | Detail |
|---|---|
| **Severity** | Critical |
| **OWASP** | A01 — Broken Access Control |
| **File** | `src/TechWayFit.Pulse.Web/Controllers/Api/ResponsesController.cs` |

`GET /{code}/participants/{participantId}/responses` returns all responses for a participant without validating the `X-Participant-Token` header. The `SubmitResponse` action in the same controller correctly calls `RequireParticipantToken`, creating an inconsistency.

**Recommendation:** Call `await RequireParticipantToken<ParticipantResponsesResponse>(session.Id, participantId)` at the start of `GetParticipantResponses`, and return the error if non-null.

---

#### SEC-004 — `WorkshopHub.Subscribe` Grants Access to Any Session Without Validation

| Field | Detail |
|---|---|
| **Severity** | Critical |
| **OWASP** | A01 — Broken Access Control |
| **File** | `src/TechWayFit.Pulse.Web/Hubs/WorkshopHub.cs` |

Any authenticated or unauthenticated SignalR client can call `Subscribe("ANY_SESSION_CODE")` and receive all real-time events for that session — including participant joins, submitted responses, and dashboard updates — without any proof of membership.

```csharp
public async Task Subscribe(string sessionCode)
{
    // No check that caller belongs to this session
    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
}
```

**Recommendation:** Before adding the connection to the group, validate that the caller holds a valid `X-Participant-Token` or `X-Facilitator-Token` for the session. Reject the subscription with a `HubException` if validation fails.

---

#### SEC-005 — SSRF via User-Controlled `OpenAiBaseUrl`

| Field | Detail |
|---|---|
| **Severity** | Critical |
| **OWASP** | A10 — Server-Side Request Forgery (SSRF) |
| **Files** | `src/TechWayFit.Pulse.Web/Controllers/AccountController.cs`, `src/TechWayFit.Pulse.AI/Http/OpenAIApiClient.cs` |

A facilitator can save any arbitrary URL as their `OpenAiBaseUrl` via `POST /account/profile/update-settings`. The value is stored in the database and used verbatim as `HttpClient.BaseAddress` for all AI HTTP calls. A malicious facilitator can direct the server to make outbound HTTP requests to internal network targets:

- `http://localhost:5432/` (database)
- `http://169.254.169.254/latest/meta-data/` (AWS IMDS)
- Internal microservices, admin panels, or corporate networks

```csharp
// OpenAIApiClient.cs — baseUrl is the raw user-supplied string
client.BaseAddress = new Uri(baseUrl);
```

**Recommendation:**
1. On save, validate that `OpenAiBaseUrl` uses `https://` scheme only.
2. Validate the hostname against an allowlist of known AI API providers (e.g., `api.openai.com`, `*.openai.azure.com`).
3. Reject URLs with private/loopback/link-local IP addresses (`127.x`, `10.x`, `192.168.x`, `169.254.x`, `::1`).
4. Log and alert on any blocked bypass attempt.

---

### HIGH

---

#### SEC-006 — No Rate Limit on OTP Verification — Brute Force Open

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A07 — Identification & Authentication Failures |
| **File** | `src/TechWayFit.Pulse.Web/Controllers/AccountController.cs` |

`MaxOtpAttemptsPerHour = 5` is applied only to **sending** OTPs, not **verifying** them. `POST /account/verify-otp` has no rate-limiting policy. A 6-digit numeric OTP has 900,000 combinations and a 10-minute expiry window — an attacker can enumerate all valid codes in seconds.

**Recommendation:** Apply a rate-limiting policy (e.g., 5 verify attempts per 10 minutes per IP) to `POST /account/verify-otp`. Additionally, track failed verification attempts per email and lock the OTP after 5 failed guesses.

---

#### SEC-007 — 30-Day Persistent Auth Cookie Overrides 8-Hour Session Timeout

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A07 — Identification & Authentication Failures |
| **File** | `src/TechWayFit.Pulse.Web/Controllers/AccountController.cs` (line 130) |

Cookie authentication is configured with `ExpireTimeSpan = 8 hours`, but the `VerifyOtp` action overrides this at sign-in time:

```csharp
var authProperties = new AuthenticationProperties
{
    IsPersistent = true,
    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // overrides the 8h setting
};
```

This creates a 30-day session window, greatly increasing the value of a stolen cookie.

**Recommendation:** Remove `IsPersistent = true` and the `ExpiresUtc` override to allow the centrally configured `ExpireTimeSpan = 8 hours` with `SlidingExpiration = true` to govern session lifetime. If "remember me" is an intentional feature, expose it as an explicit opt-in checkbox on the login form.

---

#### SEC-008 — Facilitator Tokens Never Expire

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A07 — Identification & Authentication Failures |
| **File** | `src/TechWayFit.Pulse.Web/Api/FacilitatorTokenStore.cs` |

In-memory facilitator session tokens have no TTL. A token issued at session start remains valid indefinitely (until app restart). If a token is leaked via a log entry, a network capture, or client-side JavaScript, it cannot be revoked and has no natural expiry.

```csharp
public FacilitatorAuth Create(Guid sessionId)
{
    var auth = new FacilitatorAuth(Guid.NewGuid(), Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
    // No expiry stored or checked
    _tokens.AddOrUpdate(sessionId, auth, (_, _) => auth);
    return auth;
}
```

**Recommendation:** Add an absolute expiry (e.g., 4–8 hours) to `FacilitatorAuth` and check it in `IsValid`. Remove expired tokens proactively using a background cleanup or `MemoryCache` with absolute expiration.

---

#### SEC-009 — CSP `'unsafe-inline'` and `'unsafe-eval'` Defeat XSS Protection

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A05 — Security Misconfiguration |
| **Files** | `src/TechWayFit.Pulse.Web/appsettings.json`, `src/TechWayFit.Pulse.Web/Configuration/SecurityHeadersOptions.cs` |

The default Content Security Policy includes:

```
script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net;
style-src  'self' 'unsafe-inline' ...
```

`'unsafe-inline'` allows inline `<script>` tags and event handlers to execute. `'unsafe-eval'` allows `eval()`. Together, these directives negate the XSS mitigation value of a CSP entirely. Any injected script payload will execute.

**Recommendation:** Move all inline JavaScript to external `.js` files. Implement nonce-based CSP (`'nonce-{random}'`) using ASP.NET Core middleware to allow only server-generated nonces. Remove `'unsafe-inline'` and `'unsafe-eval'`.

---

#### SEC-010 — Health Endpoints Publicly Accessible (Information Disclosure)

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A05 — Security Misconfiguration |
| **File** | `src/TechWayFit.Pulse.Web/Extensions/PulseApplicationBuilderExtensions.cs` |

`/health` and `/health/ready` return application state (database connectivity, service readiness) with no access control. This aids attacker reconnaissance — confirming the tech stack, infrastructure status, and reachability of dependencies.

```csharp
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", ...);
```

**Recommendation:** Add `.RequireAuthorization()` to health check endpoints, or restrict them to the loopback/load-balancer IP using `RequireHost`. If public health checks are required for external monitoring, return a minimal `Healthy`/`Unhealthy` status without detailed component information.

---

#### SEC-011 — `AllowedHosts: "*"` in Production Configuration

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A05 — Security Misconfiguration |
| **File** | `src/TechWayFit.Pulse.Web/appsettings.Production.json` |

`AllowedHosts` is set to `"*"`, which disables host header validation. This opens the door to host header injection attacks that can poison generated URLs in password reset/OTP emails, redirect victims, or bypass origin-based access controls.

**Recommendation:** Set `AllowedHosts` to the specific production domain(s): `"AllowedHosts": "pulse.techwayfit.com"`.

---

#### SEC-012 — Session Cookie Missing `SecurePolicy = Always`

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A05 — Security Misconfiguration |
| **File** | `src/TechWayFit.Pulse.Web/Extensions/PulseServiceCollectionExtensions.cs` |

The authentication cookie correctly enforces `CookieSecurePolicy.Always`, but the session cookie does not:

```csharp
services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // ← SecurePolicy not set; defaults to CookieSecurePolicy.None
});
```

The session cookie can be transmitted over plain HTTP, enabling interception on a non-HTTPS connection or a downgrade attack.

**Recommendation:** Add `options.Cookie.SecurePolicy = CookieSecurePolicy.Always;` to `AddSession`.

---

#### SEC-013 — DOM-Based XSS via `innerHTML` / `insertAdjacentHTML` with Server Data

| Field | Detail |
|---|---|
| **Severity** | High |
| **OWASP** | A03 — Injection (XSS) |
| **Files** | `src/TechWayFit.Pulse.Web/wwwroot/js/activity-modals.js`, `activity-poll.js`, `activity-types.js`, `form-builder.js` |

Multiple JavaScript files build HTML strings using template literals that include values fetched from the API (activity titles, poll option labels, form field labels) and inject them via `innerHTML` or `insertAdjacentHTML`. Because `'unsafe-inline'` is permitted in the CSP (see SEC-009), there is no blocking layer — a stored XSS payload in an activity title will execute in the facilitator's browser.

```javascript
// activity-modals.js — server-returned data in template literal injected as HTML
tr.innerHTML = `<td>${serverData.title}</td>`;
```

**Recommendation:** For all strings sourced from server responses or user input, use `textContent = value` (for text nodes) or `element.setAttribute()` (for attributes) instead of template-literal HTML injection. When building larger DOM structures, use `createElement` / `appendChild` chains rather than `innerHTML`.

---

### MEDIUM

---

#### SEC-014 — No Brute-Force Lockout or Alerting on OTP Verification Failures

| Field | Detail |
|---|---|
| **Severity** | Medium |
| **OWASP** | A09 — Security Logging & Monitoring Failures |
| **File** | `src/TechWayFit.Pulse.Application/Services/AuthenticationService.cs` |

`VerifyOtpAsync` logs a warning on invalid OTP but does not increment a failure counter per email address, does not lock the OTP after N failed attempts, and does not emit structured telemetry suitable for automated alerting. Combined with SEC-006 (no rate limit on verify), brute force goes completely undetected.

**Recommendation:** Track failed verification attempts per OTP record. After 5 failed guesses, mark the OTP as used/invalidated. Emit a structured log event (`SecurityEvent.OtpBruteForce`) that can be ingested by a SIEM or alert pipeline.

---

#### SEC-015 — No Payload Size Limit on `SubmitResponse`

| Field | Detail |
|---|---|
| **Severity** | Medium |
| **OWASP** | A04 — Insecure Design |
| **File** | `src/TechWayFit.Pulse.Application/Services/ResponseService.cs` |

The `payload` field in `SubmitResponseCommand` is validated only for being non-empty. No maximum length is enforced. An attacker with a valid participant token can submit arbitrarily large JSON payloads, consuming database storage and triggering unbounded memory allocation in dashboard deserialization services.

**Recommendation:** Add a maximum size check in `ResponseService.SubmitAsync` before persisting:

```csharp
if (payload.Length > 10_000)
    throw new ArgumentException("Response payload exceeds the maximum allowed size.");
```

---

#### SEC-016 — Auth Cookie `SameSite = Lax` Instead of `Strict`

| Field | Detail |
|---|---|
| **Severity** | Medium |
| **OWASP** | A04 — Insecure Design |
| **File** | `src/TechWayFit.Pulse.Web/Extensions/PulseServiceCollectionExtensions.cs` |

The authentication cookie is set to `SameSiteMode.Lax`. `Lax` sends the cookie on top-level cross-site GET navigations (e.g., following a link from another website). For a facilitator-only management surface with no legitimate cross-site embedding need, `Strict` is the safer choice and eliminates any CSRF surface on top-level navigations.

**Recommendation:** Change `options.Cookie.SameSite = SameSiteMode.Lax` to `SameSiteMode.Strict` for the auth cookie. Verify no SSO or OAuth callback flows break.

---

### LOW

---

#### SEC-017 — `OpenAiBaseUrl` Stored in Plaintext

| Field | Detail |
|---|---|
| **Severity** | Low |
| **OWASP** | A02 — Cryptographic Failures |
| **Files** | `src/TechWayFit.Pulse.Domain/Entities/FacilitatorUserData.cs`, `src/TechWayFit.Pulse.Web/Middleware/FacilitatorContextMiddleware.cs` |

The OpenAI API key is correctly protected via ASP.NET Core Data Protection. However, `OpenAiBaseUrl` is stored in plaintext. While a base URL is lower sensitivity than an API key, it can encode credentials in the URL form (`https://user:password@host/`) and is observable in plaintext in the database.

**Recommendation:** Validate and sanitise the URL on save to strip any embedded credentials. Document the intentional asymmetry (key encrypted, URL plaintext) in the code with a comment.

---

## Summary Table

| ID | Severity | Finding | OWASP |
|---|---|---|---|
| SEC-001 | **Critical** | `AiController` missing `[Authorize]` | A01 |
| SEC-002 | **Critical** | `DashboardsController` no auth or token check | A01 |
| SEC-003 | **Critical** | `GetParticipantResponses` no token check | A01 |
| SEC-004 | **Critical** | `WorkshopHub.Subscribe` grants access to any session | A01 |
| SEC-005 | **Critical** | SSRF via user-controlled `OpenAiBaseUrl` | A10 |
| SEC-006 | High | No rate limit on OTP verify — brute-force open | A07 |
| SEC-007 | High | 30-day persistent auth cookie overrides 8h timeout | A07 |
| SEC-008 | High | Facilitator tokens never expire | A07 |
| SEC-009 | High | CSP `unsafe-inline`/`unsafe-eval` defeats XSS protection | A05 |
| SEC-010 | High | Health endpoints publicly accessible | A05 |
| SEC-011 | High | `AllowedHosts: "*"` in production | A05 |
| SEC-012 | High | Session cookie missing `SecurePolicy = Always` | A05 |
| SEC-013 | High | DOM XSS via `innerHTML` with server data + permissive CSP | A03 |
| SEC-014 | Medium | No OTP failure lockout or structured alerting | A09 |
| SEC-015 | Medium | No response payload size limit | A04 |
| SEC-016 | Medium | Auth cookie `SameSite = Lax` instead of `Strict` | A04 |
| SEC-017 | Low | `OpenAiBaseUrl` stored in plaintext | A02 |

---

## Recommended Fix Priority

### Immediate (before next production deployment)
1. **SEC-001** — Add `[Authorize]` to `AiController`
2. **SEC-004** — Validate session membership in `WorkshopHub.Subscribe`
3. **SEC-005** — Validate and allowlist `OpenAiBaseUrl` on save (SSRF)
4. **SEC-006** — Add rate limiting to `POST /account/verify-otp`
5. **SEC-012** — Add `SecurePolicy = Always` to session cookie

### Short-Term (next sprint)
6. **SEC-002** — Add auth/token checks to `DashboardsController`
7. **SEC-003** — Add token check to `GetParticipantResponses`
8. **SEC-007** — Remove 30-day cookie override; use configured 8h expiry
9. **SEC-008** — Add expiry to `FacilitatorTokenStore`
10. **SEC-011** — Set `AllowedHosts` to production domain

### Medium-Term (next month)
11. **SEC-009** — Remove `unsafe-inline`/`unsafe-eval` from CSP; use nonces
12. **SEC-013** — Replace `innerHTML` DOM injection with safe DOM APIs
13. **SEC-010** — Restrict health endpoint access
14. **SEC-014** — Add OTP failure counter and lockout
15. **SEC-015** — Enforce payload size limit in `ResponseService`

### Backlog
16. **SEC-016** — Change auth cookie to `SameSite = Strict`
17. **SEC-017** — Sanitise and document `OpenAiBaseUrl` storage

---

*Generated by Security Architect review — 23 March 2026*
