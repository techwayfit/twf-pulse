# Workshop Fun App (TechWayFit) — HLD & LLD

> Purpose: Open-source, single-purpose workshop engagement app (Mentimeter-like) to **Make Workshops Fun** while producing **measurable, filterable insights** (dashboards, word cloud, 4-quadrant, charts, 5-Whys).

---

## Table of contents

- [1. Goals and non-goals](#1-goals-and-non-goals)
- [2. Personas and user journeys](#2-personas-and-user-journeys)
- [3. Key constraints](#3-key-constraints)
- [4. High-level architecture (HLD)](#4-high-level-architecture-hld)
- [5. Domain model](#5-domain-model)
- [6. Data model (EF Core + SQLite/InMemory)](#6-data-model-ef-core--sqliteinmemory)
- [7. Real-time and state machine (Facilitator-controlled flow)](#7-real-time-and-state-machine-facilitator-controlled-flow)
- [8. API contracts (LLD)](#8-api-contracts-lld)
- [9. SignalR events (LLD)](#9-signalr-events-lld)
- [10. UI screen map and component structure (LLD)](#10-ui-screen-map-and-component-structure-lld)
- [11. Rules & validations](#11-rules--validations)
- [12. Aggregation & dashboards](#12-aggregation--dashboards)
- [13. Security, privacy, and moderation](#13-security-privacy-and-moderation)
- [14. Operational concerns](#14-operational-concerns)
- [15. Implementation plan](#15-implementation-plan)

---

# 1. Goals and non-goals

## Goals

- Enable a **facilitator** to run interactive workshops where participants can:
  - Join via link/QR and fill a lightweight profile
  - Respond to facilitator-led questions **one-by-one** (only when unlocked)
  - Submit a limited number of suggestions/feedback/POVs (max **N per activity** or **N per session**)
- Provide real-time visuals:
  - Word cloud, pie/bar, rating distributions, 4-quadrant scatter, 5-Whys ladder, live feed
- Produce **measurable** results and allow filtering by participant fields.
- Run with .NET stack:
  - **ASP.NET Core + Blazor Server**
  - **EF Core** using **InMemory** (dev) and **SQLite** (small persistence)

## Non-goals

- Full whiteboarding (Miro-level) with arbitrary drawing/tools
- Long-term analytics warehouse, multi-tenant enterprise auth (future)
- Heavy AI autonomy (AI is assistive only; can be added later)

---

# 2. Personas and user journeys

## Facilitator journey

1. Create session → Provide context (title, goal, tags, constraints)
2. Configure participant join form (simple builder)
3. Create a sequence of activities/questions
4. Start session → participants join
5. Run agenda:
   - Launch Q1 → participants respond
   - Facilitator moves to next → participants can respond to Q2
6. View dashboards live → filter by participant fields
7. Export / end session

## Participant journey

1. Open join link/QR → fill join form
2. Wait in lobby until facilitator starts
3. For each question/activity:
   - Respond **once** (or limited times) while open
   - Submit suggestions/feedback up to allowed limit
4. Finish → optional final comment

---

# 3. Key constraints

1. **Participant Joining Form Builder**
   - Max **5 fields** (configurable), with simple types
   - Used for filtering/clustering (dimensions)

2. **Participant contribution limit**
   - Participant can provide max **N** points/suggestions/feedbacks/POVs
   - N should be configurable per session and optionally per activity type

3. **Facilitator-controlled pacing**
   - Participants can only respond when facilitator opens a question/activity
   - One-by-one progression: Q1 open → close → Q2 open, etc.

---

# 4. High-level architecture (HLD)

## 4.1 Proposed tech stack

- **Blazor Server UI**
  - Facilitator Console
  - Participant Experience
- **ASP.NET Core Minimal APIs** (in same host for MVP)
- **SignalR**
  - Real-time updates to dashboards + participants
- **EF Core**
  - Provider: InMemory (dev), SQLite (prod-lite)
- Optional (future): Redis for scale/TTL and multi-instance

## 4.2 Component diagram (logical)

- UI Layer (Blazor)
  - Facilitator App (Admin Console)
  - Participant App
- Application Layer
  - Session Service
  - Activity Orchestrator (state machine)
  - Response Service
  - Dashboard Aggregation Service
- Infrastructure
  - EF Core Persistence
  - SignalR Hub
  - Background Cleanup Service (TTL)

## 4.3 Data flow (real-time)

1. Facilitator opens an activity
2. Server broadcasts `ActivityStateChanged` via SignalR
3. Participants submit response
4. Server validates (session open, activity open, limit not exceeded)
5. Server stores response (EF)
6. Server updates aggregates
7. Server broadcasts `DashboardUpdated` and `ResponseReceived`

---

# 5. Domain model

## 5.1 Core entities

- **Session**
  - Context + settings
  - JoinForm schema (max 5 fields)
  - Activity sequence
  - Current activity pointer + status

- **Participant**
  - Pseudonymous identity (display name optional)
  - Join-form answers stored as **Dimensions**

- **Activity**
  - Types: Poll, Quiz, WordCloud, QnA, Rating, Quadrant, FiveWhys
  - Config per type

- **Response**
  - Participant response to an Activity
  - Stored with Dimensions snapshot for filtering

- **ContributionCounter**
  - Tracks each participant’s total points submitted (for max N)

## 5.2 Key enums

- `SessionStatus`: Draft | Live | Ended | Expired
- `ActivityStatus`: Pending | Open | Closed
- `ActivityType`: Poll | Quiz | WordCloud | QnA | Rating | Quadrant | FiveWhys
- `FieldType`: Text | Number | Dropdown | MultiSelect | Boolean

---

# 6. Data model (EF Core + SQLite/InMemory)

> Use JSON columns as TEXT for flexibility in SQLite.

## 6.1 Tables

### Sessions
- `Id` (GUID)
- `Code` (short string, unique)
- `Title`
- `Goal`
- `ContextJson` (TEXT)
- `SettingsJson` (TEXT) — includes N limits, TTL, anonymous options
- `JoinFormSchemaJson` (TEXT) — <= 5 fields
- `Status` (int)
- `CurrentActivityId` (GUID, nullable)
- `CreatedAt`, `UpdatedAt`, `ExpiresAt`

### Activities
- `Id` (GUID)
- `SessionId` (FK)
- `Order` (int)
- `Type` (int)
- `Title`
- `Prompt`
- `ConfigJson` (TEXT)
- `Status` (int)
- `OpenedAt`, `ClosedAt`

### Participants
- `Id` (GUID)
- `SessionId` (FK)
- `DisplayName` (nullable)
- `IsAnonymous` (bool)
- `DimensionsJson` (TEXT) — join form values (flattened)
- `JoinedAt`

### Responses
- `Id` (GUID)
- `SessionId` (FK)
- `ActivityId` (FK)
- `ParticipantId` (FK)
- `PayloadJson` (TEXT) — answer / point / coordinates etc.
- `DimensionsJson` (TEXT) — snapshot for filtering
- `CreatedAt`

### ParticipantCounters
- `ParticipantId` (PK/FK)
- `SessionId` (FK)
- `TotalContributions` (int)
- `UpdatedAt`

## 6.2 Indexes

- Sessions: `Code` unique, `Status`, `ExpiresAt`
- Activities: `(SessionId, Order)`, `(SessionId, Status)`
- Participants: `(SessionId, JoinedAt)`
- Responses: `(SessionId, ActivityId, CreatedAt)`, `(ParticipantId, CreatedAt)`

---

# 7. Real-time and state machine (Facilitator-controlled flow)

## 7.1 State machine

### Session
- Draft → Live → Ended
- Live → Expired (TTL)

### Activity
- Pending → Open → Closed

Rules:
- Only **one activity** can be Open at a time in a session.
- Participants can submit responses only when activity is **Open**.
- Facilitator can “re-open” a closed activity only if configured (default: no).

## 7.2 Orchestration

- Activity Orchestrator validates transitions:
  - Open next activity: closes current if open, opens target
  - Close activity: stops submissions

---

# 8. API contracts (LLD)

> For MVP, you can implement these as Minimal APIs and call them from Blazor.

## 8.1 Session APIs

### Create session
- `POST /api/sessions`
- Request:
  - title, goal, context, settings, joinFormSchema
- Response: sessionId, code

### Get session
- `GET /api/sessions/{code}`

### Start session
- `POST /api/sessions/{code}/start`

### End session
- `POST /api/sessions/{code}/end`

## 8.2 Join form schema

### Update join form (max 5 fields)
- `PUT /api/sessions/{code}/join-form`
- Validates field count <= configured max (default 5)

## 8.3 Activities

### Add activity
- `POST /api/sessions/{code}/activities`

### Reorder activities
- `PUT /api/sessions/{code}/activities/reorder`

### Open activity (facilitator pacing)
- `POST /api/sessions/{code}/activities/{activityId}/open`

### Close activity
- `POST /api/sessions/{code}/activities/{activityId}/close`

## 8.4 Participants

### Join
- `POST /api/sessions/{code}/participants/join`
- Request:
  - displayName?, isAnonymous, joinFormAnswers (validated against schema)
- Response:
  - participantId, token (ephemeral)

## 8.5 Responses

### Submit response
- `POST /api/sessions/{code}/activities/{activityId}/responses`
- Validations:
  - session is Live
  - activity is Open
  - participant belongs to session
  - participant has not exceeded **max N contributions**
  - optional: per-activity limit as well

### Get dashboard data
- `GET /api/sessions/{code}/dashboards?activityId=&filters=`

---

# 9. SignalR events (LLD)

Hub: `/hubs/workshop`

## Server → Client

- `SessionStateChanged`
  - { sessionCode, status, currentActivityId, participantCount }

- `ActivityStateChanged`
  - { sessionCode, activityId, status, openedAt, closedAt }

- `ResponseReceived`
  - { sessionCode, activityId, responseId, createdAt }

- `DashboardUpdated`
  - { sessionCode, activityId?, aggregateType, payload }

- `ParticipantJoined`
  - { sessionCode, participantCount }

## Client → Server (optional)

- `Subscribe(sessionCode)`
- `Unsubscribe(sessionCode)`

---

# 10. UI screen map and component structure (LLD)

## 10.1 Facilitator UI

### Screens

1. **Home**
   - Create session, templates, recent sessions
2. **Create Session Wizard**
   - Step 1 Context
   - Step 2 Join Form Builder (max 5 fields)
   - Step 3 Activities builder
   - Step 4 Review & Launch
3. **Live Room (Control Console)**
   - Agenda list
   - Current activity control (Open/Close/Next)
   - Participant count + QR
   - Live preview of results
4. **Dashboards**
   - Word cloud, charts, quadrant, 5-Whys
   - Filter panel (by join-form dimensions)
5. **Wrap-up**
   - AI summary (optional)
   - Export

### Components

- `SessionWizard.razor`
- `JoinFormBuilder.razor`
  - enforces max fields
- `ActivityBuilder.razor`
- `LiveConsole.razor`
- `DashboardTabs.razor`
- `FilterDrawer.razor`
- Chart wrappers (via JS interop)

## 10.2 Participant UI

### Screens

1. Join
2. Lobby (wait)
3. Activity screen (changes when facilitator moves)
4. Done

### Components

- `ParticipantJoin.razor`
- `ParticipantLobby.razor`
- `ActivityRenderer.razor` (switch by ActivityType)
- `SubmitGuard.razor` (shows locked state if activity not open)

---

# 11. Rules & validations

## 11.1 Join form (max 5 fields)

- `MaxJoinFields` is configurable (default 5)
- Allowed types: Text, Number, Dropdown, MultiSelect, Boolean
- Field ids must be unique and normalized (e.g., `team`, `role`)

## 11.2 Contribution limits

- Config:
  - `MaxContributionsPerParticipantPerSession` (N)
  - Optional: `MaxContributionsPerParticipantPerActivity`

### Enforced at submit

- Increment `ParticipantCounters.TotalContributions` on every accepted “point/suggestion/feedback/POV” response type.
- If exceeding N, reject with clear message.

## 11.3 Facilitator pacing

- Participants can submit only if:
  - session Live
  - activity Open
  - activityId matches session.CurrentActivityId (strict mode)

Strict mode is recommended to enforce one-by-one flow.

---

# 12. Aggregation & dashboards

## 12.1 Aggregates by activity type

- Poll/Quiz: counts per option (+ percentage)
- Rating: distribution + average
- WordCloud: token frequency (stopwords removed)
- QnA: list + upvotes (future)
- Quadrant: points list + quadrant counts + heatmap bins
- FiveWhys: ladder nodes + root-cause category distribution

## 12.2 Filtering

- Filters operate on `DimensionsJson` values
- Implementation options:
  - MVP: in-memory filter after query
  - Later: store normalized dimensions table for query-time filtering

---

# 13. Security, privacy, and moderation

## Privacy modes

- Facilitator only: dimensions visible to facilitator
- Aggregated: only included in aggregates; no raw display
- Anonymous: participant identity hidden; dimensions may be retained

## Security (MVP)

- Session access by code
- Facilitator “admin token” stored server-side for that session
- Participant token issued on join

---

# 14. Operational concerns

## TTL / Expiry

- Each session has `ExpiresAt`
- Background service periodically deletes expired sessions and related data (SQLite)

## Exports

- Export session package (JSON) containing:
  - session context, join schema, activities, responses, aggregates

---

# 15. Implementation plan

## Phase 1 (MVP)

- Session CRUD + Join + activity open/close
- Participant UI for Poll + WordCloud + Quadrant
- Dashboards basic (counts, cloud, scatter)

## Phase 2

- 5-Whys activity + root cause dashboards
- Contribution limits + strict pacing enforcement

## Phase 3

- Templates, exports, moderation, AI assist for question suggestions

---

## Appendix A — JSON shapes (examples)

### JoinFormSchema (max 5 fields)

```json
{
  "maxFields": 5,
  "fields": [
    {"id":"team","label":"Team","type":"Dropdown","required":true,"options":["Ops","Tech","Finance"],"useInFilters":true},
    {"id":"role","label":"Role","type":"Dropdown","required":true,"options":["Officer","Supervisor","Manager"],"useInFilters":true}
  ]
}
```

### SessionSettings

```json
{
  "maxContributionsPerParticipantPerSession": 5,
  "strictCurrentActivityOnly": true,
  "allowAnonymous": true,
  "ttlMinutes": 360
}
```

### Quadrant response payload

```json
{
  "x": 3,
  "y": 4,
  "label": "Reduce approval steps"
}
```

### Poll response payload

```json
{
  "selectedOptionId": "opt-2"
}
```

