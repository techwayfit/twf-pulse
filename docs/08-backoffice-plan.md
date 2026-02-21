# BackOffice — Plan & Implementation Document

> Status: **Planning / Pre-Implementation**  
> Author: GitHub Copilot  
> Date: 22 February 2026  
> Target Release: Phase 5

---

## 1. Purpose and Context

TechWayFit Pulse is entering go-live. The main application (`TechWayFit.Pulse.Web`) exposes only the features a **facilitator** or **participant** needs. In production, the operations team requires a separate, hardened portal to:

- Investigate and resolve user issues quickly
- Override data that users cannot change themselves (e.g. expired sessions, locked accounts, corrupted settings)
- Delete or purge data in response to GDPR / support requests
- Audit every administrative action for compliance and accountability

The solution is a new project: **`TechWayFit.Pulse.BackOffice`** — an isolated ASP.NET Core MVC web application, running on a separate port / subdomain, accessible only to authorised operators.

---

## 2. Goals and Non-Goals

### Goals
- Full operator visibility into all domain entities (users, sessions, activities, participants, responses)
- Controlled override capabilities for every critical field
- Immutable, tamper-evident audit trail stored in the database
- Role-aware access (Operator vs. Super Admin)
- Secure by default: separate auth, no shared session state with the main app

### Non-Goals
- Replace the facilitator UI — facilitators do not use BackOffice
- Real-time SignalR features — BackOffice is read/write, not live-dashboard
- Analytics or reporting dashboards (separate concern, Phase 6+)

---

## 3. Proposed Solution Structure

```
TechWayFit.Pulse.sln
 src/
   TechWayFit.Pulse.Web/              # Existing main app (unchanged)
   TechWayFit.Pulse.BackOffice/       # NEW — operator portal
   TechWayFit.Pulse.Application/      # Extended with BackOffice services
   TechWayFit.Pulse.Domain/           # Extended with audit entity
   TechWayFit.Pulse.Infrastructure/   # Extended with audit repository
   TechWayFit.Pulse.Contracts/        # Extended with BackOffice DTOs
   TechWayFit.Pulse.AI/               # Unchanged
```

### New Project: `TechWayFit.Pulse.BackOffice`

```
TechWayFit.Pulse.BackOffice/
  Program.cs                         # DI + middleware (separate from Web)
  appsettings.json
  appsettings.Production.json
  Authentication/
    BackOfficeAuthExtensions.cs      # Cookie auth for operators
    OperatorClaims.cs
  Authorization/
    PolicyNames.cs
    OperatorRoleHandler.cs
  Controllers/
    AuthController.cs                # Login / logout actions
    DashboardController.cs           # Entry overview / stats
    UsersController.cs               # Search, detail, disable, update
    SessionsController.cs            # Search, detail, force-end, extend, delete
    ActivitiesController.cs          # Detail, force-close, config edit
    ParticipantsController.cs        # List, soft-remove
    ResponsesController.cs           # List, delete
    AuditController.cs               # Audit log viewer, CSV export
    BackOfficeUsersController.cs     # Operator account management (SuperAdmin)
  Models/                            # MVC ViewModels
    Users/
      UserSearchViewModel.cs
      UserDetailViewModel.cs
    Sessions/
      SessionSearchViewModel.cs
      SessionDetailViewModel.cs
    Audit/
      AuditSearchViewModel.cs
  Views/
    Shared/
      _Layout.cshtml                 # BackOffice master layout
      _NavMenu.cshtml
      _Confirmation.cshtml           # Shared typed-confirmation partial
    Auth/
      Login.cshtml
    Dashboard/
      Index.cshtml
    Users/
      Index.cshtml                   # Search & list facilitator users
      Detail.cshtml                  # User profile + override panel
    Sessions/
      Index.cshtml                   # Search & list all sessions
      Detail.cshtml                  # Session detail + override panel
    Activities/
      Detail.cshtml                  # Activity config viewer/editor
    Participants/
      Index.cshtml                   # List participants for a session
    Responses/
      Index.cshtml                   # View all responses for an activity
    Audit/
      Index.cshtml                   # Audit log viewer with filters
    BackOfficeUsers/
      Index.cshtml                   # Operator account management
  wwwroot/
    css/
      backoffice.css                 # Operator-specific styles
```

---

## 4. Authentication & Authorisation

### Authentication
- **Separate cookie scheme** (`TechWayFit.Pulse.BackOffice.Auth`) — completely isolated from the facilitator cookie
- **Username + password** login stored in a new `BackOfficeUser` table OR pulled from an environment-configured admin credential (simpler for single-operator start)
- No OTP email — operators use a fixed credential with a long password; can be upgraded to TOTP in Phase 5.1

### Roles

| Role | Description |
|------|-------------|
| `Operator` | Read everything; limited writes (extend expiry, reset display name) |
| `SuperAdmin` | Full write access; can delete users, purge sessions, manage other operator accounts |

### Authorization Policy Examples
```csharp
// PolicyNames.cs
public static class PolicyNames
{
    public const string OperatorOrAbove = "OperatorOrAbove";
    public const string SuperAdminOnly   = "SuperAdminOnly";
}
```

All MVC controllers in BackOffice are decorated with `[Authorize(Policy = PolicyNames.OperatorOrAbove)]` at the class level as a minimum. SuperAdmin-only actions carry an additional `[Authorize(Policy = PolicyNames.SuperAdminOnly)]` attribute.

---

## 5. Audit Trail

Every write operation in BackOffice **must** create an audit record. This is non-negotiable.

### 5.1 New Domain Entity: `AuditLog`

```csharp
// TechWayFit.Pulse.Domain/Entities/AuditLog.cs

public sealed class AuditLog
{
    public Guid          Id              { get; }   // PK
    public string        OperatorId      { get; }   // BackOffice operator username
    public string        OperatorRole    { get; }   // Role at time of action
    public string        Action          { get; }   // e.g. "UpdateUserDisplayName"
    public string        EntityType      { get; }   // e.g. "FacilitatorUser"
    public string        EntityId        { get; }   // The affected record's ID
    public string?       FieldName       { get; }   // Specific field changed (nullable)
    public string?       OldValue        { get; }   // Serialised previous value
    public string?       NewValue        { get; }   // Serialised new value
    public string?       Reason          { get; }   // Operator-supplied justification
    public string        IpAddress       { get; }   // Client IP for accountability
    public DateTimeOffset OccurredAt     { get; }   // UTC timestamp
}
```

### 5.2 AuditLog Table

```sql
CREATE TABLE AuditLogs (
    Id          TEXT        NOT NULL PRIMARY KEY,
    OperatorId  TEXT        NOT NULL,
    OperatorRole TEXT       NOT NULL,
    Action      TEXT        NOT NULL,
    EntityType  TEXT        NOT NULL,
    EntityId    TEXT        NOT NULL,
    FieldName   TEXT        NULL,
    OldValue    TEXT        NULL,
    NewValue    TEXT        NULL,
    Reason      TEXT        NULL,
    IpAddress   TEXT        NOT NULL,
    OccurredAt  TEXT        NOT NULL
);

CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AuditLogs_OperatorId ON AuditLogs(OperatorId);
CREATE INDEX IX_AuditLogs_OccurredAt ON AuditLogs(OccurredAt DESC);
```

### 5.3 Audit Service Interface

```csharp
// TechWayFit.Pulse.Application/Abstractions/Services/IAuditLogService.cs

public interface IAuditLogService
{
    Task RecordAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> SearchAsync(AuditSearchQuery query, CancellationToken ct = default);
}
```

Every BackOffice service method calls `IAuditLogService.RecordAsync(...)` **before** committing changes to the database. If the audit write fails, the data write does not proceed.

---

## 6. Feature Inventory

### 6.1 User Management

| Screen | Data Shown | Editable by Operator | Editable by SuperAdmin |
|--------|-----------|----------------------|------------------------|
| User Search | Email, DisplayName, CreatedAt, LastLoginAt, Session count | — | — |
| User Detail | All fields above + UserData entries (keys, encrypted indicator) | DisplayName | DisplayName, Email |
| User Data | All `FacilitatorUserData` key-value entries | — | Add / Update / Delete key-value pairs |
| Disable Account | Active/Disabled flag (new field) | Disable | Disable / Re-enable |
| Delete Account | Hard delete (with all associated data) | — | SuperAdmin only; requires typed confirmation |

**New `FacilitatorUser` field needed:** `IsDisabled (bool)` — prevents OTP login when `true`.

### 6.2 Session Management

| Screen | Data Shown | Editable by Operator | Editable by SuperAdmin |
|--------|-----------|----------------------|------------------------|
| Session Search | Code, Title, Status, Owner email, CreatedAt, ExpiresAt, Participant count | — | — |
| Session Detail | All session fields including goal, context, settings JSON, join form schema | ExpiresAt extension | All fields including Status override, FacilitatorUserId reassignment |
| Force-end Session | Change status from `Live` → `Ended` | Yes | Yes |
| Extend Expiry | Push `ExpiresAt` forward by N days | Yes (max +30 days) | Yes (unlimited) |
| Delete Session | Hard delete session + all activities, participants, responses | — | SuperAdmin only; requires typed confirmation |

### 6.3 Activity Management

| Screen | Data Shown | Editable by Operator | Editable by SuperAdmin |
|--------|-----------|----------------------|------------------------|
| Activity Detail | Type, Status, Order, Config JSON, OpenedAt, ClosedAt | — | Config JSON (raw edit), Order, Status override |
| Force-close Activity | Change status from `Open` → `Closed` | Yes | Yes |

### 6.4 Participant Management

| Screen | Data Shown | Editable by Operator | Editable by SuperAdmin |
|--------|-----------|----------------------|------------------------|
| Participant List (per session) | Name, Email, JoinedAt, join form answers, response count | — | — |
| Remove Participant | Soft-delete participant and their responses | — | SuperAdmin only |

### 6.5 Response Management

| Screen | Data Shown | Editable by Operator | Editable by SuperAdmin |
|--------|-----------|----------------------|------------------------|
| Response List (per activity) | ParticipantId, payload JSON, CreatedAt | — | — |
| Delete Response | Hard delete a single response | — | SuperAdmin only |

### 6.6 Session Groups

| Screen | Data Shown | Editable by Operator | Editable by SuperAdmin |
|--------|-----------|----------------------|------------------------|
| Group List | All groups, owner, session count | — | Rename, reassign owner, delete |

### 6.7 Audit Log Viewer

- List all audit records with filtering by: OperatorId, EntityType, EntityId, Action, date range
- No edits — read-only
- Export to CSV (operator or SuperAdmin)

### 6.8 BackOffice User Management (SuperAdmin only)

- List operator accounts
- Create / deactivate operator accounts
- Change operator role (Operator ↔ SuperAdmin)
- All changes audited

---

## 7. New Domain Fields Required

The following additions to existing domain entities are needed to support BackOffice features.

| Entity | New Field | Type | Purpose |
|--------|-----------|------|---------|
| `FacilitatorUser` | `IsDisabled` | `bool` | Prevent login when disabled by operator |
| `FacilitatorUser` | `DisabledAt` | `DateTimeOffset?` | When account was disabled |
| `FacilitatorUser` | `DisabledReason` | `string?` | Operator-provided reason |
| `Session` | `IsAdminLocked` | `bool` | Prevent facilitator from editing when operator lock is active |
| `Participant` | `IsRemoved` | `bool` | Soft-delete flag (participant remains for audit; excluded from dashboards) |
| `Participant` | `RemovedAt` | `DateTimeOffset?` | When removed |

---

## 8. New BackOffice Entity: `BackOfficeUser`

```csharp
// TechWayFit.Pulse.Domain/Entities/BackOfficeUser.cs

public sealed class BackOfficeUser
{
    public Guid           Id          { get; }
    public string         Username    { get; }
    public string         PasswordHash { get; }   // BCrypt
    public string         Role        { get; }    // "Operator" | "SuperAdmin"
    public bool           IsActive    { get; }
    public DateTimeOffset CreatedAt   { get; }
    public DateTimeOffset? LastLoginAt { get; }
}
```

For the initial release, 1–2 `SuperAdmin` accounts are seeded from environment variables (`BO_ADMIN_USER`, `BO_ADMIN_PASS_HASH`) to avoid storing secrets in appsettings.

---

## 9. Technology Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| UI framework | ASP.NET Core MVC (Controllers + Razor Views) | No SignalR / real-time needed; standard request/response is sufficient; simpler, no WebSocket overhead |
| Auth | Cookie-based (ASP.NET Core, separate scheme) | Simple, secure; isolated from main app cookie |
| Password storage | BCrypt (`BCrypt.Net-Next`) | Industry standard for password hashing |
| Shared infrastructure | Same `PulseDbContext` / SQLite database | No sync complexity; BackOffice is read-heavy, low concurrency |
| Port / deployment | Separate Kestrel port (e.g. 5001) or separate Docker service | Prevents accidental exposure through the same domain |
| CSS | Bootstrap 5.3 + minimal `backoffice.css` | Match existing design system; operator UX is functional, not branded |
| Audit write strategy | Synchronous, within same DB transaction as the data write | Guarantees audit is never lost if data write occurs |

---

## 10. Security Hardening

1. **Network isolation**: BackOffice runs on a private port or private network. Never exposed to the public internet — accessed via VPN or bastion only.
2. **IP allowlisting middleware**: Optional middleware to restrict access to specific operator IP ranges.
3. **Rate limiting**: Login attempts limited to 5 per minute per IP.
4. **No shared cookies**: `TechWayFit.Pulse.BackOffice.Auth` cookie has `HttpOnly`, `Secure`, `SameSite=Strict`, 8-hour expiry.
5. **Audit on login/logout**: All successful and failed login attempts are audit-logged.
6. **All overrides require a `Reason` field**: Operators must enter a justification for any write operation.
7. **Confirmation dialogs for destructive actions**: Delete flows require typing the entity code/ID to confirm.
8. **No CORS**: BackOffice serves no public APIs.

---

## 11. Implementation Phases

### Phase 5.0 — Foundation (1–2 sprints)

| ID | Task | Layer | Priority |
|----|------|-------|----------|
| BO-01 | Add `AuditLog` entity + EF migration + `IAuditLogService` | Domain / Infrastructure | P0 |
| BO-02 | Add `BackOfficeUser` entity + EF migration + password hash seeding | Domain / Infrastructure | P0 |
| BO-03 | Add new fields to `FacilitatorUser` and `Participant` (`IsDisabled`, `IsRemoved`, etc.) | Domain / Infrastructure | P0 |
| BO-04 | Create `TechWayFit.Pulse.BackOffice` project and add to solution | Web | P0 |
| BO-05 | Configure separate cookie auth + role-based policies | BackOffice | P0 |
| BO-06 | Login page + operator session management | BackOffice | P0 |
| BO-07 | Main layout, nav menu, dashboard stub | BackOffice | P0 |

### Phase 5.1 — User & Session Management (1–2 sprints)

| ID | Task | Layer | Priority |
|----|------|-------|----------|
| BO-08 | `IBackOfficeUserService` + `IBackOfficeSessionService` interfaces and implementations | Application / Infrastructure | P0 |
| BO-09 | User search + detail pages (read) | BackOffice | P0 |
| BO-10 | User: disable / re-enable (with audit) | BackOffice | P0 |
| BO-11 | User: update DisplayName / Email (SuperAdmin, with audit) | BackOffice | P0 |
| BO-12 | User: manage `FacilitatorUserData` entries (SuperAdmin, with audit) | BackOffice | P0 |
| BO-13 | Session search + detail pages (read) | BackOffice | P0 |
| BO-14 | Session: force-end | BackOffice | P0 |
| BO-15 | Session: extend expiry | BackOffice | P0 |
| BO-16 | Session: full field override (SuperAdmin, with audit) | BackOffice | P1 |
| BO-17 | Session: delete (SuperAdmin, with typed confirmation + audit) | BackOffice | P1 |

### Phase 5.2 — Activity, Participant, Response Management (1 sprint)

| ID | Task | Layer | Priority |
|----|------|-------|----------|
| BO-18 | Activity detail + force-close | BackOffice | P1 |
| BO-19 | Activity: config JSON edit (SuperAdmin, with audit) | BackOffice | P1 |
| BO-20 | Participant list per session (read) | BackOffice | P1 |
| BO-21 | Participant: soft-remove (SuperAdmin, with audit) | BackOffice | P1 |
| BO-22 | Response list per activity (read) | BackOffice | P1 |
| BO-23 | Response: delete (SuperAdmin, with audit) | BackOffice | P2 |

### Phase 5.3 — Audit Viewer + BackOffice User Management (1 sprint)

| ID | Task | Layer | Priority |
|----|------|-------|----------|
| BO-24 | Audit log viewer with filters | BackOffice | P0 |
| BO-25 | Audit log CSV export | BackOffice | P1 |
| BO-26 | BackOffice user management (SuperAdmin: create, deactivate, change role) | BackOffice | P1 |
| BO-27 | IP allowlist middleware | BackOffice | P2 |
| BO-28 | Login attempt rate limiter | BackOffice | P1 |

---

## 12. Key Contracts (DTOs)

```csharp
// TechWayFit.Pulse.Contracts/BackOffice/

// Search / list
public record UserSearchQuery(string? EmailContains, string? NameContains, bool? IsDisabled, int Page, int PageSize);
public record UserSummary(Guid Id, string Email, string DisplayName, bool IsDisabled, DateTimeOffset CreatedAt, int SessionCount);

public record SessionSearchQuery(string? CodeContains, string? TitleContains, SessionStatus? Status, Guid? FacilitatorUserId, int Page, int PageSize);
public record SessionSummary(Guid Id, string Code, string Title, SessionStatus Status, string OwnerEmail, DateTimeOffset ExpiresAt, int ParticipantCount);

// Override requests — always carry operator justification
public record DisableUserRequest(Guid UserId, string Reason);
public record UpdateUserDisplayNameRequest(Guid UserId, string NewDisplayName, string Reason);
public record ExtendSessionExpiryRequest(Guid SessionId, int AdditionalDays, string Reason);
public record ForceEndSessionRequest(Guid SessionId, string Reason);
public record DeleteSessionRequest(Guid SessionId, string ConfirmationCode, string Reason);

// Audit
public record AuditSearchQuery(string? OperatorId, string? EntityType, string? EntityId, DateTimeOffset? From, DateTimeOffset? To, int Page, int PageSize);
public record AuditLogSummary(Guid Id, string OperatorId, string Action, string EntityType, string EntityId, string? FieldName, string? Reason, DateTimeOffset OccurredAt);
```

---

## 13. Logging and Monitoring

- BackOffice uses the same **Serilog** setup as the main app
- All audit writes are also logged via Serilog at `Information` level with structured properties
- Failed login attempts are logged at `Warning` level
- Destructive actions (deletes) are logged at `Warning` level

---

## 14. Testing Strategy

| Layer | Approach |
|-------|----------|
| `IAuditLogService` | Unit tests: verify audit records are written with correct fields |
| `IBackOfficeUserService` | Unit tests: disable, update, delete flows; verify `IsDisabled` flag |
| `IBackOfficeSessionService` | Unit tests: force-end, extend expiry, delete |
| Auth / policies | Integration tests: unauthenticated requests return 401; `Operator` role cannot call `SuperAdmin` endpoints |
| BackOffice controllers / views | Integration tests with `WebApplicationFactory`; controller action tests for confirmation flows and redirect behaviour |

---

## 15. Open Questions / Decisions Needed

| # | Question | Default Assumption |
|---|----------|--------------------|
| Q1 | Should BackOffice authenticate via same OTP email system or use a separate username/password? | Separate username + password (operators are internal; OTP email is for facilitators) |
| Q2 | Should BackOffice run in the same Docker container as the main Web app (different port), or a separate container? | Separate container in docker-compose; cleaner isolation |
| Q3 | Should `AuditLog` rows be stored in the same `pulse.db` SQLite file or a separate file? | Same `pulse.db`; simpler for now; revisit if audit volume becomes significant |
| Q4 | Do we need GDPR "right to erasure" bulk delete (all data for a given facilitator email)? | Add as SuperAdmin feature in Phase 5.2 |
| Q5 | Should operators receive email notifications when a SuperAdmin deletes their data? | Out of scope for Phase 5; log only |

---

## 16. Sequence: Typical Operator Workflow (Override with Audit)

```
Operator                 BackOffice Blazor         IBackOfficeSessionService     IAuditLogService      PulseDbContext
   |                           |                              |                        |                     |
   |-- Search session "PULSE-1234" -->                        |                        |                     |
   |                   |-- GetSessionAsync("PULSE-1234") -->  |                        |                     |
   |                   |<-- SessionDetail -------------------|                        |                     |
   |-- Click "Extend Expiry" (enter reason) -->               |                        |                     |
   |                   |-- ExtendExpiryAsync(req) ---------> |                        |                     |
   |                   |                              [begin DB transaction]           |                     |
   |                   |                              |-- RecordAsync(auditEntry) --> |                     |
   |                   |                              |                        |-- INSERT AuditLog -------> |
   |                   |                              |-- UPDATE Session.ExpiresAt -----------------------> |
   |                   |                              [commit transaction]            |                     |
   |<-- Success toast --|                              |                        |                     |
```

---

## 17. Summary

**`TechWayFit.Pulse.BackOffice`** is a dedicated, isolated operator portal that gives the operations team full visibility and controlled override capabilities over all entities in the system, with a mandatory, tamper-evident audit trail on every write.

It is not a replacement for the facilitator UI. It is a scalpel for production incidents.

**Recommended start:** Phase 5.0 (foundation + auth) followed immediately by Phase 5.1 (user + session management), as those cover 90% of go-live support scenarios.
