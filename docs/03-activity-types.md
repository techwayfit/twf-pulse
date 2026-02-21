# Activity Types — TechWayFit Pulse

> Last Updated: February 2026 | Version: 2.0

---

## 1. Status Summary

| Status | Count | Activity Types |
|--------|-------|---------------|
| Fully Complete | 5 (62.5%) | Poll, Rating, WordCloud, GeneralFeedback, Quadrant |
| Deferred / Hidden | 1 (12.5%) | FiveWhys |
| Not Started | 2 (25%) | Quiz, QnA |

**Enum values** (`ActivityType`):
```
Poll = 0, Quiz = 1, WordCloud = 2, QnA = 3, Rating = 4, Quadrant = 5, FiveWhys = 6, GeneralFeedback = 7
```

---

## 2. Completed Activity Types

---

### Poll — 100% Complete

**Purpose**: Single or multiple choice voting for quick consensus and prioritization.

**Files**:

| Component | Path |
|-----------|------|
| Config model | `Domain/Models/ActivityConfigs/PollConfig.cs` |
| Response model | `Domain/Models/ResponsePayloads/PollResponse.cs` |
| Participant UI | `Web/Components/Participant/Activities/PollActivity.razor` |
| Dashboard UI | `Web/Components/Dashboards/PollDashboard.razor` |
| Dashboard service | `Application/Services/PollDashboardService.cs` |
| Creation page | `Web/Pages/Facilitator/CreatePollActivity.razor` |
| JS visualization | `Web/wwwroot/js/poll-dashboard.js` |

**Features**:
- Single or multiple choice selection
- Custom "Other" option with write-in text
- Option randomization
- Real-time bar chart (Chart.js)
- Vote distribution by participant dimensions
- Config: min/max selections, anonymity

**Response Payload**:
```json
{
  "selectedOptionIds": ["option-1", "option-2"],
  "customOptionText": "Custom response if enabled"
}
```

---

### Rating — 100% Complete

**Purpose**: Numerical feedback on defined scales (1–5, 1–10) with optional comments.

**Files**:

| Component | Path |
|-----------|------|
| Config model | `Domain/Models/ActivityConfigs/RatingConfig.cs` |
| Response model | `Domain/Models/ResponsePayloads/RatingResponse.cs` |
| Participant UI | `Web/Components/Participant/Activities/RatingActivity.razor` |
| Dashboard UI | `Web/Components/Dashboards/RatingDashboard.razor` |
| Dashboard service | `Application/Services/RatingDashboardService.cs` |
| Creation page | `Web/Pages/Facilitator/CreateRatingActivity.razor` |

**Features**:
- Stars, slider, or button input types
- Optional comment field (required or optional)
- Custom scale labels (min, max, midpoint)
- Distribution histogram, average, median, min, max
- Comment aggregation by rating level

**Response Payload**:
```json
{
  "rating": 4,
  "scale": 5,
  "comment": "Optional feedback text"
}
```

---

### WordCloud — 100% Complete

**Purpose**: Collect keywords or short phrases to visualize common themes.

**Files**:

| Component | Path |
|-----------|------|
| Config model | `Domain/Models/ActivityConfigs/WordCloudConfig.cs` |
| Response model | `Domain/Models/ResponsePayloads/WordCloudResponse.cs` |
| Participant UI | `Web/Components/Participant/Activities/WordCloudActivity.razor` |
| Dashboard UI | `Web/Components/Dashboards/WordCloudDashboard.razor` |
| Dashboard service | `Application/Services/WordCloudDashboardService.cs` |
| Creation page | `Web/Pages/Facilitator/CreateWordCloudActivity.razor` |
| JS visualization | `Web/wwwroot/js/wordcloud-dashboard.js` |

**Features**:
- Word frequency visualization (Chart.js WordCloud plugin)
- Stop words filtering, case sensitivity options
- Multiple submissions per participant (configurable)
- Word length constraints (min/max)
- Three view modes: Cloud, Chart, List
- Real-time updates via SignalR

**Response Payload**:
```json
{
  "text": "keyword or short phrase"
}
```

---

### GeneralFeedback — 100% Complete

**Purpose**: Categorized open-ended feedback collection.

**Files**:

| Component | Path |
|-----------|------|
| Config model | `Domain/Models/ActivityConfigs/GeneralFeedbackConfig.cs` |
| Response model | `Domain/Models/ResponsePayloads/GeneralFeedbackResponse.cs` |
| Participant UI | `Web/Components/Participant/Activities/GeneralFeedbackActivity.razor` |
| Dashboard UI | `Web/Components/Dashboards/GeneralFeedbackDashboard.razor` |
| Dashboard service | `Application/Services/GeneralFeedbackDashboardService.cs` |
| Creation page | `Web/Pages/Facilitator/CreateGeneralFeedbackActivity.razor` |

**Features**:
- Long-form text submissions
- Optional category selection
- Character count display, min/max length validation
- Anonymous submission option
- Category-based filtering and grouping
- Multiple submissions per participant (configurable)

**Common Category Patterns**:
- Retrospective: "What Went Well", "What Didn't", "Action Items"
- Feedback: "Features", "Bugs", "Documentation", "Support"

**Response Payload**:
```json
{
  "text": "Feedback text content",
  "category": "category-id",
  "isAnonymous": true,
  "characterCount": 142
}
```

---

### Quadrant — 100% Complete

**Purpose**: 2D mapping of items across two dimensions for prioritization (e.g., Impact vs Effort).

**Files**:

| Component | Path |
|-----------|------|
| Config model | `Domain/Models/ActivityConfigs/QuadrantConfig.cs` |
| Response model | `Contracts/Responses/QuadrantPoint.cs` |
| Participant UI | `Web/Components/Participant/Activities/QuadrantActivity.razor` |
| Dashboard UI | `Web/Components/Dashboards/QuadrantDashboard.razor` |
| Dashboard service | Methods in `DashboardService.cs` |
| Creation modal | Quadrant modal in `Views/Facilitator/AddActivities.cshtml` |

**Features**:
- Text label input (participant names their item)
- Dual-slider X/Y coordinate input with configurable axis labels
- Visual quadrant preview with real-time position feedback
- Configurable per-participant point limit
- Live scatter plot visualization in facilitator dashboard
- Real-time updates via SignalR

**Configuration Schema**:
```json
{
  "xAxisLabel": "Effort Required",
  "yAxisLabel": "Strategic Impact",
  "topLeftLabel": "High Impact, Low Effort (Quick Wins)",
  "topRightLabel": "High Impact, High Effort (Long-term Bets)",
  "bottomLeftLabel": "Low Impact, Low Effort (Nice to Have)",
  "bottomRightLabel": "Low Impact, High Effort (Avoid)",
  "scale": 10,
  "allowLabels": true,
  "maxLabelLength": 100,
  "maxPointsPerParticipant": 1
}
```

**Response Payload**:
```json
{
  "x": 2.5,
  "y": 4.0,
  "label": "Reduce approval steps"
}
```

---

## 3. Deferred Activity Types

---

### FiveWhys — Deferred, Hidden from UI

**Purpose**: Guided root cause analysis using the "5 Whys" technique.

**Decision**: The `ActivityType.FiveWhys` enum value (`= 6`) is reserved but all UI entry points (creation modal/button) are **hidden from the facilitator creation UI** pending full implementation. Responses of this type fall back to `GenericActivity.razor` in the participant UI.

**Expected Response Payload** (when implemented):
```json
{
  "initialProblem": "Why do approvals take too long?",
  "chain": [
    { "level": 1, "question": "Why do approvals take too long?", "answer": "Managers are overloaded" },
    { "level": 2, "question": "Why are managers overloaded?", "answer": "No delegation policy exists" }
  ],
  "depthReached": 5,
  "completedAt": "2026-01-15T10:30:00Z"
}
```

**Complexity**: Very High — multi-step wizard, dynamic question generation, tree visualization, optional AI integration.

---

## 4. Not Started Activity Types

---

### Quiz — 0% Complete

**Purpose**: Knowledge assessment with correct answers and scoring.

**Priority**: Phase 3

**Expected Configuration**:
- Multiple questions with options and correct answers
- Show/hide correct answers and explanations
- Allow retry, randomize questions/options
- Passing score threshold, max responses per participant

**Expected Response Payload**:
```json
{
  "answers": [
    {
      "questionId": "q1",
      "selectedOptionId": "a2",
      "isCorrect": true,
      "timeSpent": 12.5
    }
  ],
  "score": 100,
  "totalQuestions": 1,
  "correctAnswers": 1
}
```

---

### QnA — 0% Complete

**Purpose**: Question collection with upvoting/downvoting and moderation.

**Priority**: Phase 3

**Expected Configuration**:
- Allow upvoting/downvoting, anonymous question submission
- Facilitator moderation/approval
- Max question length, max questions per participant

**Expected Response Payload**:
```json
{
  "type": "question",
  "text": "Will the new process integrate with CRM?",
  "isAnonymous": true
}
```

**Complexity**: High — requires voting system (separate response records for votes), moderation UI, ranking logic.

---

## 5. Implementation Patterns

All completed activity types follow these consistent patterns. Use **Poll** as the reference implementation.

### File Locations

```
TechWayFit.Pulse.Domain/
  Models/
    ActivityConfigs/     [ActivityType]Config.cs
    ResponsePayloads/    [ActivityType]Response.cs

TechWayFit.Pulse.Application/
  Abstractions/Services/ I[ActivityType]DashboardService.cs
  Services/              [ActivityType]DashboardService.cs

TechWayFit.Pulse.Contracts/
  Responses/             [ActivityType]DashboardResponse.cs

TechWayFit.Pulse.Web/
  Components/
    Participant/Activities/   [ActivityType]Activity.razor
    Dashboards/               [ActivityType]Dashboard.razor
  Pages/Facilitator/          Create[ActivityType]Activity.razor
  wwwroot/js/                 [activity-type]-dashboard.js
```

### Step-by-Step for New Activity Types

1. **Domain models** (30 min) — `[Type]Config.cs` + `[Type]Response.cs`
2. **Dashboard service** (1–2 hr) — interface + implementation with aggregation logic
3. **Dashboard response DTO** (15 min) — `[Type]DashboardResponse.cs` in Contracts
4. **Participant component** (2–4 hr) — `[Type]Activity.razor`
5. **Dashboard component** (2–4 hr) — `[Type]Dashboard.razor`
6. **JS visualization** (1–3 hr, if needed) — Chart.js integration
7. **Facilitator creation page** (2–3 hr) — `Create[Type]Activity.razor`
8. **Register services** (5 min) — `Program.cs` DI registration

**Estimated total**: 1–2 days per activity type depending on complexity.

### Best Practices

- Use Bootstrap 5.3 classes in all Razor components (mobile-first)
- Use SignalR for real-time dashboard updates; listen for `ResponseReceived` and `DashboardUpdated`
- Store strongly-typed DTOs in Contracts; return them from dashboard services
- Use Chart.js for all charting; store chart instances in global JS objects for disposal
- Follow SQL projection for dashboard queries — avoid loading full response entities
- Add `[Parameter]` attributes for `SessionCode` and `ActivityId` in dashboard components

---

## 6. Shared Infrastructure (Available to All Activity Types)

**Real-time**:
- `WorkshopHub` events: `ResponseReceived`, `ActivityStateChanged`, `DashboardUpdated`

**Data access**:
- `IResponseRepository` — response CRUD
- `IActivityRepository` — activity management
- `IParticipantRepository` — participant data
- `ISessionRepository` — session management

**Validation** (in `ResponseService`):
- Activity must be `Open` to accept responses
- Contribution counter checked against `maxResponsesPerParticipant`
- Response payload validated against activity config

**Endpoints**:
- `POST /api/sessions/{code}/activities/{activityId}/responses` — submit response
- `GET /api/sessions/{code}/dashboard` — get dashboard data
- Activity-specific dashboard endpoints for each type

---

## 7. Known Gaps and Technical Debt

| Issue | Impact | Status |
|-------|--------|--------|
| Validation services not separated per type | Low — currently in `ResponseService` | Deferred |
| Aggregation services not separated from dashboard services | Low | Deferred |
| Inline styles in some activity components | Styling inconsistency | TECH-04, Not Started |
| Quiz and QnA activity types missing entirely | Feature gap | FEAT-03, FEAT-04 |
| FiveWhys deferred due to complexity | Hidden from facilitators | FEAT-02, Done (hidden) |
