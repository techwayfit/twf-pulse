# Solution Architecture — TechWayFit Pulse

> Last Updated: February 2026

---

## 1. Overview

TechWayFit Pulse is a .NET 8 Blazor Server application for running interactive workshops with real-time activities (polls, word clouds, quadrant matrices, etc.). The system is a single-host web application combining MVC, Blazor Server, SignalR, and an optional AI pipeline.

---

## 2. Solution Structure

```
TechWayFit.Pulse.sln
 src/
   TechWayFit.Pulse.Web/           # Blazor Server host + MVC + APIs + SignalR
   TechWayFit.Pulse.Application/   # Orchestration, use-cases, validation
   TechWayFit.Pulse.Domain/        # Entities, enums, domain rules
   TechWayFit.Pulse.Infrastructure/ # EF Core, repositories, SignalR adapters
   TechWayFit.Pulse.Contracts/     # DTOs, API/SignalR contracts
   TechWayFit.Pulse.AI/            # AI services (OpenAI, Intelligent, MLNet, Mock)
 tests/
   TechWayFit.Pulse.Tests/         # Unit + light integration tests
```

### Project Responsibilities

**`TechWayFit.Pulse.Web`**
- Blazor pages, components, layouts, and static assets
- MVC controllers and views for static (non-interactive) pages
- API controllers: sessions, activities, participants, responses, dashboards, session groups, templates
- SignalR hub for real-time updates (`WorkshopHub` at `/hubs/workshop`)
- Composition root: DI container, middleware pipeline, configuration
- Authentication: cookie-based auth (`TechWayFit.Pulse.Auth` cookie), OTP email login
- Background services: `AIProcessingHostedService`, `TemplateInitializationHostedService`

**`TechWayFit.Pulse.Application`**
- Orchestration services: `SessionService`, `ActivityService`, `ResponseService`, `SessionGroupService`, `SessionTemplateService`
- Validation pipeline: join form, pacing rules, contribution limits
- Dashboard aggregation services: `PollDashboardService`, `WordCloudDashboardService`, `RatingDashboardService`, `GeneralFeedbackDashboardService`, `DashboardService` (Quadrant)
- AI quota management: `AiQuotaService` (free-tier quota, BYOK bypass)

**`TechWayFit.Pulse.Domain`**
- Core entities: `Session`, `Activity`, `Participant`, `Response`
- Enums: `ActivityType`, `SessionStatus`, `ActivityStatus`
- Domain rules: state transitions, contribution limits, join schema constraints

**`TechWayFit.Pulse.Infrastructure`**
- EF Core `PulseDbContext`, entity mappings, SQLite migrations
- Manual SQL scripts for SQL Server in `Scripts/V1.0/`, `Scripts/V1.1/`
- `AIWorkQueue` — singleton `Channel<Func<CancellationToken, Task>>` for async AI processing
- SignalR event publishing wrappers

**`TechWayFit.Pulse.AI`**
- `SessionAIService` — OpenAI-based session generation
- `IntelligentSessionAIService` — NLP keyword-based generation (no API key)
- `MLNetSessionAIService` — ML.NET machine-learning generation (no API key)
- `ParticipantAIService` / `FacilitatorAIService` — response analysis + facilitator prompts
- `MockParticipantAIService` / `MockFacilitatorAIService` — stubs for dev/testing
- `PiiSanitizer` — sanitizes context documents before AI calls

**`TechWayFit.Pulse.Contracts`**
- API request/response DTOs
- SignalR event payload models
- AI result DTOs: `ParticipantAnalysisResult`, `FacilitatorPromptResult`, `AICallTelemetry`
- Shared constants: route strings, hub event names

---

## 3. Architecture Layers

```
 Browser
    |
    | HTTP (MVC/API) + WebSocket (SignalR)
    |
 TechWayFit.Pulse.Web
    |
    | Interfaces only
    |
 TechWayFit.Pulse.Application
    |
    | Domain types only
    |
 TechWayFit.Pulse.Domain
    ^
    | Implements application interfaces
    |
 TechWayFit.Pulse.Infrastructure  <--  TechWayFit.Pulse.AI
    |
    | EF Core / SQLite
    |
   Database
```

Dependency rule: **Web → Application → Domain; Infrastructure implements Application interfaces; AI project implements AI interfaces defined in Application.**

---

## 4. UI Strategy: MVC vs Blazor

| Page Type | Technology | Rationale |
|-----------|-----------|-----------|
| Home, Create Session, Edit Session | MVC (Controller + Razor View) | Static content, no WebSocket needed, better SEO |
| Live Facilitator Console | Blazor Server | Real-time SignalR events, interactive controls |
| Participant Activity View | Blazor Server | Real-time activity state, response submission |
| Admin / Session Groups | MVC | CRUD forms, no real-time requirement |

---

## 5. Real-Time Layer (SignalR)

**Hub**: `WorkshopHub` at `/hubs/workshop`

**Events broadcast to clients**:

| Event | Trigger | Payload |
|-------|---------|---------|
| `SessionStateChanged` | Session start / end | `{ sessionId, status }` |
| `ActivityStateChanged` | Activity open / close | `{ activityId, status, openedAt, closedAt }` |
| `ParticipantJoined` | New participant joins | `{ participantId, name, joinedAt }` |
| `ResponseReceived` | Response submitted | `{ activityId, responseId }` |
| `DashboardUpdated` | Aggregate recalculated or AI insight ready | `{ activityId, aggregateType, payload }` |

Clients are organized into **SignalR groups by session code**, so only participants in the same session receive events.

AI insights are broadcast as `DashboardUpdated` with `AggregateType = "AIInsight"`.

---

## 6. Authentication

- **Method**: Cookie-based auth using the `TechWayFit.Pulse.Auth` cookie
- **Login flow**: OTP sent via email → facilitator enters OTP → cookie set (8-hour sliding expiry)
- **Middleware**: `FacilitatorTokenMiddleware` and `FacilitatorContextMiddleware` inject facilitator identity into the request context
- **Participant access**: Participants use a short-lived participant session token stored in `localStorage`; no login required

---

## 7. Database Strategy

| Environment | Provider | Management |
|-------------|----------|-----------|
| Development / Test | `InMemory` | `EnsureCreated()` — no migrations |
| Production (lite) | SQLite | EF Core migrations (`dotnet ef migrations add / database update`) |
| Enterprise | SQL Server | Manual SQL scripts in `Infrastructure/Scripts/V{major}.{minor}/` |

**Configuration switch** (in `appsettings.json` / env vars):

```json
{
  "Pulse": {
    "UseInMemory": false,
    "ConnectionStrings": {
      "DefaultConnection": "Data Source=App_Data/pulse.db"
    }
  }
}
```

**EF Core conventions**:
- DbContext: `PulseDbContext`
- Tables: PascalCase plural (EF default)
- PKs: `Id`; FKs: `<Entity>Id`
- JSON columns: suffix `Json` (e.g., `JoinFormSchemaJson`)
- Indexes: `IX_<Table>_<Column>`

---

## 8. AI Layer

AI is **optional**. The app runs fully without AI — mock services are used when AI is disabled or unconfigured.

**Background processing** (prevents HTTP latency on activity close):
1. Facilitator closes activity → controller **enqueues** item on `IAIWorkQueue` (non-blocking)
2. `AIProcessingHostedService` (background `IHostedService`) dequeues items and calls AI services
3. Results broadcast to facilitator via `DashboardUpdated` SignalR event

**Provider selection** (configured via `AI:Provider`):

| Provider | Requires API Key | Notes |
|----------|-----------------|-------|
| `OpenAI` | Yes (`AI:OpenAI:ApiKey`) | GPT-4o-mini (default), supports Azure OpenAI endpoint |
| `Intelligent` | No | NLP + TF-IDF keyword extraction, pure C# |
| `MLNet` | No | ML.NET text featurization and sentiment analysis |
| `Mock` | No | Stub responses, always available |

**Quota**: 5 free AI session generations/month per facilitator (configurable). Facilitators with BYOK (own API key in profile) bypass quota.

**Resilience** (OpenAI HTTP client): 2 retries with exponential backoff + jitter, circuit breaker (70% failure / 120s window), 60s attempt timeout, 120s total timeout.

---

## 9. Logging

Logging uses **Serilog** with two sinks:
- Console (structured JSON in production, readable in dev)
- File (rolling daily, `App_Data/Logs/pulse-{date}.txt`, 30-day retention)

Request logging via `UseSerilogRequestLogging()` middleware.

---

## 10. Middleware Pipeline Order

```
UseHttpsRedirection
UseStaticFiles
UseSerilogRequestLogging
UseRouting
UseSession
UseAuthentication
FacilitatorTokenMiddleware
FacilitatorContextMiddleware
UseAuthorization
MapControllers
MapHub("/hubs/workshop")
MapBlazorHub
MapControllerRoute
MapRazorPages
MapFallbackToPage("/_Host")
```

---

## 11. Naming Conventions

### C# Code

| Element | Convention | Example |
|---------|-----------|---------|
| Classes / Records / Enums | PascalCase | `SessionService` |
| Interfaces | `I` prefix + PascalCase | `ISessionRepository` |
| Methods / Properties | PascalCase | `OpenActivityAsync` |
| Private fields | `_camelCase` | `_repository` |
| Local variables | camelCase | `sessionCode` |
| Async methods | suffix `Async` | `GetSessionAsync` |

### API Routes

- Base path: `/api`
- Segments: kebab-case, plural resources
- Examples: `POST /api/sessions`, `GET /api/sessions/{code}`, `POST /api/sessions/{code}/activities/{activityId}/open`

### DTOs

- Requests: `CreateSessionRequest`, `JoinParticipantRequest`
- Responses: `SessionSummaryResponse`, `DashboardResponse`
- JSON keys: `camelCase`; enums serialized as strings (e.g., `"Quadrant"`)

### Blazor Components

- Components: `PascalCase.razor`
- Top-level screens in `Pages/`; reusable UI in `Components/`; layout in `Shared/`

### CSS

- Custom classes: `kebab-case`
- Bootstrap classes used as-is

---

## 12. Design Principles

1. **Facilitation-first** — facilitator controls pacing; participants respond only when unlocked
2. **Single host for MVP** — Blazor + APIs + SignalR in one process
3. **Strict layering** — Web → Application → Domain; Infrastructure implements interfaces
4. **Explicit contracts** — all DTOs and hub events in `Contracts` project
5. **Session as unit of scope** — all entities (activities, participants, responses) are session-bound
6. **One open activity at a time** — enforced in domain and API layer
7. **AI optional** — mock services used when AI disabled; no hard dependency on external APIs
8. **SRP / DI / KISS** — single reason to change, all dependencies injected, favour simple state transitions
