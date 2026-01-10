# Pulse (TechWayFit) — Solution Design

> Scope: solution structure, project layout, naming conventions, and design principles for the MVP.

---

## 1. Solution structure (projects)

> Target: .NET 8, Blazor Server + Minimal APIs + SignalR, EF Core with InMemory (dev) and SQLite (lite prod).

```
TechWayFit.Pulse.sln
  src/
    TechWayFit.Pulse.Web/              # Blazor Server host + Minimal APIs + SignalR
    TechWayFit.Pulse.Application/      # Orchestration, use-cases, validation rules
    TechWayFit.Pulse.Domain/           # Entities, enums, value objects, domain rules
    TechWayFit.Pulse.Infrastructure/   # EF Core, repositories, SignalR adapters
    TechWayFit.Pulse.Contracts/        # DTOs, API/SignalR contracts, validation models
  tests/
    TechWayFit.Pulse.Tests/            # Unit + light integration tests (fast)
```

### Project responsibilities

- `TechWayFit.Pulse.Web`
  - Blazor pages, components, layouts, static assets.
  - Minimal API endpoints (session, activities, participants, responses, dashboards).
  - SignalR hub for real-time updates.
  - Composition root: DI container, middleware, config.
- `TechWayFit.Pulse.Application`
  - Orchestration services (SessionService, ActivityOrchestrator, ResponseService).
  - Validation pipeline for join form, pacing rules, contribution limits.
  - Aggregation services for dashboards.
- `TechWayFit.Pulse.Domain`
  - Core entities and enums (Session, Activity, Participant, Response).
  - Domain rules (state transitions, limits, join schema limits).
- `TechWayFit.Pulse.Infrastructure`
  - EF Core DbContext, mappings, migrations (SQLite).
  - Persistence helpers and repository adapters.
  - SignalR event publishing wrappers.
- `TechWayFit.Pulse.Contracts`
  - API request/response DTOs.
  - SignalR event payload models.
  - Shared constants (route strings, hub event names).

### System architecture overview (from system diagram)

- **People**: Facilitator creates session, controls pacing, reviews insights; participants join via QR/link, respond only when unlocked, and see real-time visuals.
- **Experience layer**: Blazor Server UI with session wizard, activity controls (next/open/close), live dashboards + filters, export + wrap-up; participant experience includes join + lobby, single activity screen at a time, submit responses, done/final thought.
- **Real-time + rules engine**: ASP.NET Core host with SignalR hub broadcasting activity opened/closed, response received, dashboard updated; Minimal APIs for sessions, join form, activities, join, submit response, dashboards, exports, end session; rules include Activity Orchestrator (state machine), strict mode (current activity only), contribution guard (max N per participant), join form validation (<= 5 fields), aggregation outputs (cloud, charts, quadrant).
- **Data + lifecycle**: EF Core persistence with SQLite (prod-lite) and InMemory (dev) for sessions, activities, participants, responses, counters, aggregates; TTL cleanup service for auto-expire + optional export-before-delete; optional future scale-out via Redis, SignalR backplane, and hot aggregates cache.

### Constraints (diagram callouts)

- Join form builder limited to 5 fields (configurable).
- Each participant has max N points/feedback/POVs.
- Respond only when facilitator opens the next item.

---

## 2. Naming conventions

### 2.1 General C# conventions

- Namespaces: `TechWayFit.Pulse.<Layer>` (e.g., `TechWayFit.Pulse.Application`).
- Types: `PascalCase` for classes, records, enums, interfaces.
- Interfaces: `IServiceName`, `IRepositoryName`, `IHubClient`.
- Methods/props: `PascalCase`, locals: `camelCase`.
- Async methods: `VerbAsync` (e.g., `OpenActivityAsync`).

### 2.2 API routes and DTOs

- Routes: kebab-case for segments, plural resources.
- Base path: `/api`.
- Examples:
  - `POST /api/sessions`
  - `GET /api/sessions/{code}`
  - `PUT /api/sessions/{code}/join-form`
  - `POST /api/sessions/{code}/activities/{activityId}/open`
- DTOs:
  - Requests: `CreateSessionRequest`, `JoinParticipantRequest`.
  - Responses: `SessionSummaryResponse`, `DashboardResponse`.

### 2.3 SignalR events

- Hub: `/hubs/workshop`
- Event names: `PascalCase` with noun+verb.
- Examples:
  - `SessionStateChanged`
  - `ActivityStateChanged`
  - `ResponseReceived`
  - `DashboardUpdated`
  - `ParticipantJoined`

### 2.4 Blazor components

- Components: `PascalCase.razor`
- Screens: `Home.razor`, `SessionWizard.razor`, `LiveConsole.razor`.
- Reusable components: `JoinFormBuilder.razor`, `DashboardTabs.razor`.
- Folder grouping:
  - `Pages/` for top-level screens
  - `Components/` for reusable UI
  - `Shared/` for layout and nav

### 2.5 EF Core and database

- DbContext: `PulseDbContext`
- Tables: `PascalCase` plural (EF default), or explicit `snake_case` if needed.
- Keys: `Id` for PK, `<Entity>Id` for FK.
- JSON columns: suffix `Json` (e.g., `JoinFormSchemaJson`).
- Index names: `IX_<Table>_<Column>` (default EF format).

### 2.6 JSON payloads

- JSON keys: `camelCase`.
- Join form fields: `id` values are lowercase, kebab or snake avoided for simplicity.
- Enums serialized as strings (e.g., `"Quadrant"`).

---

## 3. Design principles

### 3.1 Product and UX

- Facilitation first: the facilitator controls pacing and activity flow.
- Low-friction join: minimal fields, fast join via code/QR.
- Measurable output: every activity yields structured, filterable data.
- Guardrails: enforce contribution limits and one-activity-at-a-time rules.

### 3.2 Architecture

- Single host for MVP: Blazor + Minimal APIs + SignalR in one app.
- Strict layering: Web depends on Application; Application depends on Domain; Infrastructure implements interfaces.
- Use explicit contracts: DTOs and hub events in `TechWayFit.Pulse.Contracts`.

### 3.3 Data and state

- Session is the unit of scope; all entities are session-bound.
- One open activity at a time; participants can submit only when open.
- Dimension snapshots: store join-form answers with each response for filtering.

### 3.4 Real-time behavior

- Server is source of truth; clients render based on `SessionStateChanged` and `ActivityStateChanged`.
- Aggregates update on write; dashboards refresh via `DashboardUpdated`.
- Throttle high-frequency updates if needed (future).

### 3.5 Validation and safety

- Validate on server: session status, activity status, participation limits, join-form schema.
- Reject invalid actions with clear error messages.
- Prefer explicit error results over silent failures.

### 3.6 Operability

- TTL-based cleanup for sessions and related data.
- Exportable JSON package for portability.
- No heavy external dependencies in MVP.

### 3.7 Model boundaries

- **Domain entities** are persistence-agnostic and represent business rules and invariants.
- **DB entities** (EF Core models) are optimized for storage concerns (JSON columns, indexes, denormalized snapshots).
- Mapping between DB and Domain lives in `TechWayFit.Pulse.Infrastructure` (or dedicated mappers in `TechWayFit.Pulse.Application`).

### 3.8 API request/response consistency

- Standardize request/response shapes for all endpoints.
- Proposed response envelope (example):
  - `data` for success payload
  - `errors` for validation or domain failures
  - `meta` for pagination or extra context (optional)

### 3.9 Engineering principles

- **SRP**: one reason to change per class/service.
- **DI**: all infrastructure and external dependencies injected via interfaces.
- **DRY**: avoid duplicate rules/validations; centralize in Application layer.
- **KISS**: prefer simple workflows and explicit state transitions.

---

## 4. Open questions / TBD

- Final solution name and root namespace (confirm `TechWayFit.Pulse`).
- Conventions for DB naming (EF default vs explicit snake_case).
- Authentication plan for facilitator admin token (MVP: server-stored token).
