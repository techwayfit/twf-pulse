# Functional Details — TechWayFit Pulse

> Last Updated: February 2026

---

## 1. What Is TechWayFit Pulse?

TechWayFit Pulse is an interactive workshop engagement platform. A **facilitator** creates a session, builds an agenda of activities, and controls the pacing live. **Participants** join via a code or QR link and respond to activities in real time. Results appear instantly in the facilitator's dashboard.

### Core Value Proposition
- Replace passive presentations with live, structured interaction
- Collect measurable, structured data in every workshop
- Give facilitators real-time visibility and control
- Support any meeting format: retrospectives, town halls, planning, training, research

---

## 2. Core Concepts

### Session
A session is the top-level container for a workshop.

| Field | Description |
|-------|-------------|
| `Code` | Short join code (e.g. `PULSE-1234`) shared with participants |
| `Title` | Session name |
| `Goal` | What the session aims to achieve |
| `Context` | Background information (used for AI generation) |
| `Status` | `Draft` → `Live` → `Ended` → `Expired` |
| `CurrentActivityId` | Which activity is currently open (null if none) |
| `FacilitatorUserId` | Owner of the session |
| `GroupId` | Optional session group for organisation |
| `SessionStart` / `SessionEnd` | Planned or actual timestamps |
| `ExpiresAt` | TTL — session and data deleted after this |
| `Settings` | JSON settings block (e.g. strict mode, max participants) |
| `JoinFormSchema` | Up to 5 custom fields participants fill on join |

### Activity
A single interactive step within a session (e.g. "Vote on top concerns").

| Field | Description |
|-------|-------------|
| `Type` | `Poll`, `Rating`, `WordCloud`, `GeneralFeedback`, `Quadrant`, `QnA`, `Quiz`, `FiveWhys` |
| `Status` | `Draft` → `Open` → `Closed` |
| `Order` | Display order in the agenda |
| `Config` | JSON configuration specific to the activity type |
| `OpenedAt` / `ClosedAt` | Timestamps when facilitator opened / closed |

**Rule**: Only one activity can be `Open` at a time per session. Opening a new activity auto-closes the current one.

### Participant
A person who joins a live session.

- Identified by name + email + any custom join form fields
- Receives a short-lived participant token stored in `localStorage`
- Can only submit responses while the relevant activity is `Open`
- Contribution limits enforced per activity (configurable max responses)

### Response
A participant's submission to an open activity.

- Payload schema varies by activity type (see [03-activity-types.md](./03-activity-types.md))
- Join-form dimension data is stored with each response for filtering
- Stored immutably — no edits after submission
- Aggregated in real time by dashboard services

---

## 3. Session Lifecycle

```
Create Session (Draft)
        |
        | Facilitator clicks "Start Session"
        v
    Live Session
        |                           |
        | Activities open/close     | Participants join and respond
        v                           v
    Ended Session
        |
        | TTL expires (ExpiresAt)
        v
    Expired (auto-deleted)
```

### State Transitions

| From | To | Trigger |
|------|----|---------|
| `Draft` | `Live` | `PUT /api/sessions/{code}/start` |
| `Live` | `Ended` | `PUT /api/sessions/{code}/end` |
| `Live` / `Ended` | `Expired` | Background TTL cleanup |

---

## 4. Facilitator Workflow

### 1. Create Session (4-step wizard)

**Step 1 — Context Setup**
- Session title, goal, context, planned start/end, facilitator settings (strict mode, max participants)

**Step 2 — Join Form Builder**
- Up to 5 custom fields (text, email, select, checkbox)
- Fields are shown to participants on join; answers stored with every response for filtering

**Step 3 — Activity Builder**
- Add activities from a palette (Poll, Rating, Word Cloud, General Feedback, Quadrant)
- Configure each activity type's specific settings (options, scale, axis labels, etc.)
- Re-order drag-and-drop; delete unwanted
- Alternatively: use a **session template** or **AI generation** to auto-populate

**Step 4 — Review & Launch**
- Review full session configuration
- Copy join link / QR code to share with participants
- Launch (transitions session to `Live`)

### 2. Live Console

During a live session the facilitator sees:
- Session status + participant count (real-time)
- Activity agenda with status badges (Draft / Live / Closed)
- Controls: Open, Close on each activity
- Response count per activity (live)
- Dashboard panel (opens when an activity is closed or on demand)
- QR code for participant joining
- End Session button

### 3. Dashboard Review

After closing an activity the facilitator views the live dashboard:
- Real-time aggregated results for the activity type
- Dimension filtering using join-form fields (e.g., filter poll results by "Department")
- AI insights panel (if AI enabled) — themes, summaries, facilitator prompts broadcast via SignalR

---

## 5. Participant Workflow

1. **Receive link** — Facilitator shares a URL (`/participant/join?code=PULSE-1234`) or QR code
2. **Enter code** — If no code in URL, participant types it manually
3. **Fill join form** — Name, email, and any custom fields the facilitator configured
4. **Wait in lobby** — "Waiting for activity to start..." until the facilitator opens one
5. **Respond** — Activity UI loaded; participant submits response
6. **Wait again** — After submission: "Waiting for the next activity..."
7. **Session ends** — "Thank you for participating" screen

Participants see only the currently open activity. They cannot see other responses or the facilitator dashboard.

---

## 6. Join Form Builder

- Facilitators define up to 5 fields (configurable limit)
- Supported field types: `text`, `email`, `select` (dropdown), `checkbox`
- Fields can be required or optional
- Join form schema is stored as JSON on the session (`JoinFormSchemaJson`)
- Join-form answers are stored with every response → enables dimension-based filtering in dashboards (e.g., "Show poll results for Engineering only")

---

## 7. Session Groups

Sessions can be organised into a **hierarchical group structure** (folders/categories).

- Groups have a name, description, and optional parent group
- Sessions can be assigned to a group at creation or later
- Group management UI available in the facilitator portal
- Useful for organising sessions by team, quarter, project, or client
- Managed via `ISessionGroupService` and `/api/session-groups` endpoints

---

## 8. Session Templates

Templates allow facilitators to spin up a pre-configured session in seconds.

### System Templates (Built-in)

| Template | Purpose | Activities |
|----------|---------|-----------|
| Retro Sprint Review | Agile retrospective | Pulse poll + word cloud + action items |
| Ops Pain Points | Operational improvement | Impact/Effort quadrant + general feedback |
| Product Discovery | Feature ideation | Idea word cloud + prioritisation poll |
| Incident Review | Post-incident analysis | Root cause feedback + fixes poll |

### Custom Templates

- Facilitators can save any session configuration as a template
- Templates include: session settings, join form schema, and full activity list with configs
- Seeded at startup from JSON files in `App_Data/Templates/` via `TemplateInitializationHostedService`

### API Endpoints

| Method | Endpoint | Description |
|--------|---------|-------------|
| `GET` | `/api/templates` | List all templates (filter by category) |
| `GET` | `/api/templates/{id}` | Get template detail |
| `POST` | `/api/templates` | Create custom template |
| `POST` | `/api/sessions/from-template/{id}` | Create session from template |

---

## 9. AI-Assisted Session Generation

Facilitators can generate a complete workshop agenda automatically.

### Inputs
- Session title, goal, workshop type
- Optional: participant types, sprint backlogs, incident reports, product documentation (context documents)

### What It Generates
- 3-7 activities with titles, prompts, and configurations tailored to the goal
- Activity types selected contextually (e.g. a retrospective gets a Poll + WordCloud + Quadrant)

### How It Works
1. Facilitator fills the AI generation form on the Create Session page
2. Request sent to `POST /api/sessions/generate-with-ai`
3. Controller calls selected AI provider (OpenAI, Intelligent, or MLNet)
4. Generated activities populated into the session wizard for review/edit before saving
5. Facilitator can accept as-is or modify individual activities

### Provider Options

| Provider | Quality | Api Key | Speed |
|----------|---------|---------|-------|
| OpenAI (`gpt-4o-mini`) | Best | Required (BYOK or quota) | Medium |
| Intelligent (NLP) | Good | None | Fast |
| MLNet (ML.NET) | Good | None | Fast |
| Mock | Stub | None | Instant |

### Quota
- 5 free AI generations per facilitator per month (default, configurable)
- Facilitators with their own API key (BYOK) bypass quota limits

---

## 10. Representative Use Cases

Below are representative examples of how Pulse is used across industries.

**Corporate & Business**
- Strategic planning and OKR workshops
- Team retrospectives (agile sprint reviews)
- Performance calibration sessions
- Change management readiness assessments
- Innovation brainstorming and idea voting
- Budget prioritization (effort vs. impact)

**Technology & Engineering**
- Technical design reviews and architecture decisions
- DevOps post-mortems and incident reviews
- Agile estimation / planning poker
- Security training knowledge checks
- API design workshops
- Release planning and dependency mapping

**Education & Training**
- University lecture comprehension checks
- Professional certification knowledge assessments
- Student orientation and expectation setting
- Faculty development workshops
- Online course webinars with live engagement

**Healthcare**
- Medical team huddles and triage prioritization
- Clinical trial investigator alignment
- Pharmacy protocol reviews
- Wellness program goal-setting

**Community & Events**
- Town hall meetings (citizen input)
- Non-profit board strategic planning
- Conference and seminar audience engagement
- Hackathon team idea voting

---

## 11. Business Rules

| Rule | Detail |
|------|--------|
| One open activity at a time | Opening a new activity auto-closes the previous one |
| Respond only when open | Participants cannot submit to a closed or draft activity |
| Contribution limits | Each activity has configurable `maxResponsesPerParticipant` |
| Join form limit | Up to 5 fields (configurable in settings) |
| Session TTL | Sessions expire after `ExpiresAt`; data is deleted on expiry |
| Facilitator ownership | Only the session creator can control pacing and view the full dashboard |
| PII in AI | Context documents are sanitized via `PiiSanitizer` before sending to AI |
