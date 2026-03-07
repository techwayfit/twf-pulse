# Session Templates

This directory contains system template definitions in JSON format. On application startup, these templates are automatically loaded into the database and then moved to the `installed/` subdirectory.

## How It Works

1. **Add Template**: Place a new `.json` file in this directory
2. **Start Application**: Templates are automatically processed on startup
3. **Auto-Install**: Successfully processed templates are moved to `installed/` folder
4. **Database Storage**: Template data is stored in the `SessionTemplates` table
5. **Update Existing**: If a template with the same name already exists, it is updated in-place

## Template File Format

Each JSON file should follow this structure:

```json
{
  "name": "Template Name",
  "description": "Short description of the template",
  "category": "Retrospective|ProductDiscovery|IncidentReview|TeamBuilding|Training|Custom",
  "iconEmoji": "🔄",
  "config": {
    "title": "Default session title",
    "goal": "Session goal/objective",
    "context": "Additional context for the session",
    "settings": {
      "durationMinutes": 60,
      "allowAnonymous": false,
      "allowLateJoin": true,
      "showResultsDuringActivity": true
    },
    "joinFormSchema": {
      "fields": [
        {
          "name": "name",
          "label": "Your Name",
          "type": "text",
          "required": true
        }
      ]
    },
    "activities": [
      {
        "order": 1,
        "type": "Poll|Quiz|WordCloud|QnA|Rating|Quadrant|GeneralFeedback|Break",
        "title": "Activity Title",
        "prompt": "Activity prompt/question",
        "durationMinutes": 5,
        "config": {
          // Activity-specific configuration — see sections below
        }
      }
    ]
  }
}
```

### Template-Level Fields

| Field | Type | Required | Notes |
|---|---|---|---|
| `name` | string | Yes | Used as unique identifier for update detection |
| `description` | string | Yes | Shown on the template selection screen |
| `category` | string | Yes | One of: `Retrospective`, `ProductDiscovery`, `IncidentReview`, `TeamBuilding`, `Training`, `Custom` |
| `iconEmoji` | string | Yes | Emoji shown on the template card |
| `config` | object | Yes | Full session configuration — see below |

### Session `settings` Fields

| Field | Type | Default | Notes |
|---|---|---|---|
| `durationMinutes` | integer | 120 | Session TTL in minutes; also shown during creation |
| `allowAnonymous` | boolean | true | Whether anonymous participation is allowed |
| `allowLateJoin` | boolean | true | Informational; displayed to facilitator |
| `showResultsDuringActivity` | boolean | false | Informational; displayed to facilitator |

> **Note**: Only `allowAnonymous` and `durationMinutes` directly affect the created `SessionSettings` domain object. `allowLateJoin` and `showResultsDuringActivity` are stored in the template config for display purposes.

### Join Form `fields` Types

Each entry in `joinFormSchema.fields` must have:

| Field | Type | Required | Notes |
|---|---|---|---|
| `name` | string | Yes | Unique field identifier (also used as the field ID) |
| `label` | string | Yes | Display label shown to participants |
| `type` | string | Yes | One of: `text`, `number`, `select` (or `dropdown`), `multiselect`, `boolean` (or `checkbox`) |
| `required` | boolean | No | Whether the field is mandatory (default: false) |
| `options` | array | Conditional | Required when `type` is `select`, `dropdown`, or `multiselect` |

#### Display Name — Required vs Optional

The join page always shows a **Display Name** field. Whether it is mandatory is controlled by whether a field with `"name": "displayName"` and `"required": true` exists in the join form.

```json
// Mandatory display name — anonymous joining is blocked
{ "name": "displayName", "label": "Display Name", "type": "text", "required": true }

// No displayName field in the schema — display name shown as optional, anonymous join allowed
```

> Use `"name": "displayName"` (exact, case-insensitive) as the field's reserved identifier. Any other `name` value (e.g. `"name"`) is treated as a regular custom field and does **not** enforce display name or block anonymous joining.

---

## Supported Activity Types

8 activity types are available for standard templates:

| Type | Purpose | Requires Config |
|---|---|---|
| **Poll** | Single or multiple choice voting | Yes (options required) |
| **Quiz** | Knowledge assessment with correct answers | Yes (options + correct index required) |
| **WordCloud** | Word/phrase collection visualized as a cloud | Optional |
| **QnA** | Q&A forum with upvoting | Optional |
| **Rating** | Numeric rating scale with optional comments | Optional |
| **Quadrant** | Item scoring on a 2-axis bubble chart | Yes (items + axis labels required) |
| **GeneralFeedback** | Open-ended text with optional categories | Optional |
| **Break** | Timed break with countdown and ready signal | Optional |

> **PRO-only types** (`FiveWhys`, `AiSummary`) require an active AI integration and must not be included in system templates.

---

## Activity-Specific Configurations

### Poll
**Purpose**: Participants vote on predefined options (single or multiple choice).

```json
{
  "order": 1,
  "type": "Poll",
  "title": "Which release approach do you prefer?",
  "prompt": "Select the deployment strategy that best fits our team",
  "durationMinutes": 5,
  "config": {
    "options": [
      { "id": "blue-green", "label": "Blue-Green Deploy", "description": "Zero downtime swap" },
      { "id": "canary", "label": "Canary Release", "description": "Gradual rollout" },
      { "id": "feature-flags", "label": "Feature Flags", "description": null }
    ],
    "allowMultiple": false,
    "minSelections": 1,
    "maxSelections": 1,
    "allowCustomOption": false,
    "customOptionPlaceholder": "Other (please specify)",
    "randomizeOrder": false,
    "showResultsAfterSubmit": false,
    "maxResponsesPerParticipant": 1
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `options` | array | — | **Required.** Each option needs `id` (string) and `label` (string). `description` is optional |
| `allowMultiple` | boolean | false | Allow selecting more than one option |
| `minSelections` | integer | 1 | Minimum number of options to select |
| `maxSelections` | integer | 1 (or options.length when allowMultiple) | Maximum options selectable |
| `allowCustomOption` | boolean | false | Show an "Other" free-text write-in option |
| `customOptionPlaceholder` | string | `"Other (please specify)"` | Placeholder for the write-in field |
| `randomizeOrder` | boolean | false | Shuffle option display order per participant |
| `showResultsAfterSubmit` | boolean | false | Show live results immediately after a participant votes |
| `maxResponsesPerParticipant` | integer | 1 | How many times a participant can vote |

---

### Quiz
**Purpose**: Knowledge check with predefined correct answers. Uses the same options format as Poll, with an additional `correctOptionIndex` to mark the right answer.

```json
{
  "order": 2,
  "type": "Quiz",
  "title": "API Design Knowledge Check",
  "prompt": "What HTTP status code indicates a resource was successfully created?",
  "durationMinutes": 3,
  "config": {
    "options": [
      { "id": "opt-200", "label": "200 OK", "description": null },
      { "id": "opt-201", "label": "201 Created", "description": null },
      { "id": "opt-204", "label": "204 No Content", "description": null },
      { "id": "opt-404", "label": "404 Not Found", "description": null }
    ],
    "correctOptionIndex": 1,
    "maxResponsesPerParticipant": 1
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `options` | array | — | **Required.** Same structure as Poll options |
| `correctOptionIndex` | integer | — | **Required.** Zero-based index of the correct option |
| `maxResponsesPerParticipant` | integer | 1 | How many attempts a participant gets |

---

### WordCloud
**Purpose**: Collect single words or short phrases; displayed as a word-frequency cloud.

```json
{
  "order": 1,
  "type": "WordCloud",
  "title": "Sprint in One Word",
  "prompt": "Describe this sprint in one word",
  "durationMinutes": 3,
  "config": {
    "maxWords": 1,
    "minWordLength": 3,
    "maxWordLength": 50,
    "placeholder": "Enter a word",
    "allowMultipleSubmissions": false,
    "maxSubmissionsPerParticipant": 1,
    "stopWords": ["the", "and", "is", "a", "an"],
    "caseSensitive": false
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `maxWords` | integer | 3 | Max number of words per submission (min: 1) |
| `minWordLength` | integer | 3 | Minimum character count per word (min: 1) |
| `maxWordLength` | integer | 50 | Maximum character count per word |
| `placeholder` | string | `"Enter a word or short phrase"` | Input placeholder text |
| `allowMultipleSubmissions` | boolean | false | Allow a participant to submit more than once |
| `maxSubmissionsPerParticipant` | integer | 1 | Max submissions when `allowMultipleSubmissions` is true |
| `stopWords` | array | Default list | Words filtered out of cloud rendering |
| `caseSensitive` | boolean | false | When false, "Word" and "word" are treated as the same |

---

### QnA (Questions & Answers)
**Purpose**: Participants submit questions which others can upvote. Facilitator can address top questions live.

```json
{
  "order": 3,
  "type": "QnA",
  "title": "Open Q&A",
  "prompt": "What questions do you have for the team?",
  "durationMinutes": 10,
  "config": {
    "allowAnonymous": true,
    "maxQuestionsPerParticipant": 3,
    "allowUpvoting": true,
    "maxQuestionLength": 300,
    "requireModeration": false
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `allowAnonymous` | boolean | true | Hide submitter identity from other participants |
| `maxQuestionsPerParticipant` | integer | 3 | Max questions a single participant can submit |
| `allowUpvoting` | boolean | true | Participants can upvote questions they want answered |
| `maxQuestionLength` | integer | 300 | Max character count per question |
| `requireModeration` | boolean | false | When true, questions are hidden until approved by the facilitator (reserved for future use) |

---

### Rating
**Purpose**: Participants give a numeric score on a configurable scale, with optional free-text comment.

```json
{
  "order": 4,
  "type": "Rating",
  "title": "Presentation Clarity",
  "prompt": "How clear was the presenter's explanation?",
  "durationMinutes": 3,
  "config": {
    "scale": 5,
    "minLabel": "1 - Very Unclear",
    "maxLabel": "5 - Very Clear",
    "midpointLabel": "3 - Somewhat Clear",
    "allowComments": true,
    "commentRequired": false,
    "commentPlaceholder": "Tell us more (optional)",
    "displayType": "Buttons",
    "showAverageAfterSubmit": false,
    "maxResponsesPerParticipant": 1
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `scale` | integer | 5 | Max rating value. Must be 2–10 |
| `minLabel` | string | `"1 - Low"` | Label shown at the minimum end |
| `maxLabel` | string | `"{scale} - High"` | Label shown at the maximum end |
| `midpointLabel` | string | null | Optional label for the middle value |
| `allowComments` | boolean | true | Show a free-text comment field |
| `commentRequired` | boolean | false | Require a comment before submitting |
| `commentPlaceholder` | string | `"Tell us more (optional)"` | Placeholder for the comment field |
| `displayType` | string | `"Buttons"` | UI control: `"Buttons"`, `"Slider"`, or `"Stars"` |
| `showAverageAfterSubmit` | boolean | false | Show the running average after a participant submits |
| `maxResponsesPerParticipant` | integer | 1 | How many times a participant can rate |

---

### Quadrant
**Purpose**: Participants score a facilitator-defined list of items on two configurable axes (e.g. Impact vs. Effort). Results are plotted on a bubble chart grouped into four quadrant zones.

> **This is an item-scoring activity, not a drag-and-drop canvas.** The facilitator steps through each item one at a time; participants select scores for the X and Y axes. The dashboard aggregates responses into a bubble chart.

```json
{
  "order": 3,
  "type": "Quadrant",
  "title": "Initiative Prioritization",
  "prompt": "Score each initiative on Impact and Effort",
  "durationMinutes": 15,
  "config": {
    "xAxisLabel": "Effort",
    "yAxisLabel": "Impact",
    "xScoreOptions": [
      { "value": "1", "label": "Minimal", "description": "Little effort required" },
      { "value": "3", "label": "Moderate", "description": null },
      { "value": "5", "label": "Significant", "description": "Large cross-team effort" },
      { "value": "8", "label": "Major", "description": null },
      { "value": "13", "label": "Massive", "description": "Months of work" }
    ],
    "yScoreOptions": [],
    "items": [
      "Migrate to new CI/CD pipeline",
      "Introduce automated testing",
      "Refactor authentication service",
      "Improve onboarding documentation"
    ],
    "q1Label": "Quick Wins",
    "q2Label": "Major Projects",
    "q3Label": "Fill-Ins",
    "q4Label": "Thankless Tasks",
    "bubbleSizeMode": "Proportional",
    "allowNotes": false
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `xAxisLabel` | string | `"Complexity"` | Label for the horizontal axis |
| `yAxisLabel` | string | `"Effort"` | Label for the vertical axis |
| `xScoreOptions` | array | 1–10 integers | Selectable score values for the X axis. Each entry: `value` (string, parseable as double), `label` (string, shown in dropdown), `description` (string or null) |
| `yScoreOptions` | array | `[]` | Selectable score values for the Y axis. **When empty, reuses `xScoreOptions`** |
| `items` | array | `[]` | **Required.** List of items/topics to score. Max 200. The facilitator steps through these one at a time |
| `q1Label` | string | `"Quick Wins"` | Label for top-left quadrant (low X, high Y) |
| `q2Label` | string | `"Major Projects"` | Label for top-right quadrant (high X, high Y) |
| `q3Label` | string | `"Fill-Ins"` | Label for bottom-left quadrant (low X, low Y) |
| `q4Label` | string | `"Thankless Tasks"` | Label for bottom-right quadrant (high X, low Y) |
| `bubbleSizeMode` | string | `"Proportional"` | `"Proportional"` (bubble size reflects response count) or `"Uniform"` |
| `allowNotes` | boolean | false | Whether participants can add a free-text note per item |

**Common `xScoreOptions` Presets**:
- **Simple 1–5**: `[{"value":"1"},{"value":"2"},{"value":"3"},{"value":"4"},{"value":"5"}]`
- **Fibonacci**: `[{"value":"1"},{"value":"2"},{"value":"3"},{"value":"5"},{"value":"8"},{"value":"13"}]`
- **T-shirt sizes**: `[{"value":"1","label":"XS"},{"value":"2","label":"S"},{"value":"3","label":"M"},{"value":"5","label":"L"},{"value":"8","label":"XL"}]`

**Common Quadrant Patterns**:
- **Impact / Effort** — Q1: Quick Wins, Q2: Major Projects, Q3: Fill-Ins, Q4: Thankless Tasks
- **Urgency / Importance** (Eisenhower) — Q1: Schedule, Q2: Do First, Q3: Delegate, Q4: Eliminate
- **Risk / Likelihood** — Q1: Monitor, Q2: Act Now, Q3: Low Priority, Q4: Mitigate

---

### GeneralFeedback
**Purpose**: Open-ended free-text feedback, optionally organized by categories (e.g., "What Went Well", "Improvements").

```json
{
  "order": 4,
  "type": "GeneralFeedback",
  "title": "Action Items",
  "prompt": "What should we do differently next sprint?",
  "durationMinutes": 10,
  "config": {
    "categoriesEnabled": true,
    "categories": [
      { "id": "process", "label": "Process", "icon": "⚙️" },
      { "id": "collaboration", "label": "Collaboration", "icon": "🤝" },
      { "id": "tools", "label": "Tools", "icon": "🔧" },
      { "id": "quality", "label": "Quality", "icon": "✨" }
    ],
    "requireCategory": false,
    "showCharacterCount": true,
    "maxLength": 1000,
    "minLength": 10,
    "placeholder": "Share your thoughts...",
    "allowAnonymous": false,
    "maxResponsesPerParticipant": 5
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `categoriesEnabled` | boolean | false | Enable category selection |
| `categories` | array | `[]` | **Required when `categoriesEnabled` is true.** Each entry: `id` (string), `label` (string), `icon` (emoji string, optional) |
| `requireCategory` | boolean | false | Force participants to pick a category before submitting |
| `showCharacterCount` | boolean | true | Show live character counter |
| `maxLength` | integer | 1000 | Max characters per response (minimum 10) |
| `minLength` | integer | 10 | Min characters required |
| `placeholder` | string | `"Share your thoughts, problems, or suggestions..."` | Input placeholder text |
| `allowAnonymous` | boolean | true | Allow anonymous submissions |
| `maxResponsesPerParticipant` | integer | 5 | How many responses a participant can submit |

**Common Category Patterns**:
- Retrospective: `"What Went Well"`, `"What Didn't Go Well"`, `"Action Items"`
- Feedback: `"Features"`, `"Bugs"`, `"Documentation"`, `"Support"`
- Ideas: `"Process"`, `"Product"`, `"People"`, `"Technology"`

---

### Break
**Purpose**: Scheduled break with a configurable message and optional countdown timer. Participants can signal they are back via a "Ready" button.

```json
{
  "order": 5,
  "type": "Break",
  "title": "Coffee Break",
  "prompt": null,
  "durationMinutes": 15,
  "config": {
    "message": "Take a short break. We'll resume in 15 minutes!",
    "durationMinutes": 15,
    "showCountdown": true,
    "allowReadySignal": true
  }
}
```

**Properties**:

| Property | Type | Default | Notes |
|---|---|---|---|
| `message` | string | `"Take a short break. We'll resume shortly!"` | Message displayed to participants during the break |
| `durationMinutes` | integer | 15 | Duration of the break; drives the countdown timer |
| `showCountdown` | boolean | true | Whether to show a live countdown timer to participants |
| `allowReadySignal` | boolean | true | Whether participants can click "Ready" to signal they are back |

---

## Updating Existing Templates

To update an existing system template:
1. Modify the JSON file in the `installed/` folder
2. Copy it back to this directory (root, not `installed/`)
3. Restart the application

The system detects the template by `name` and updates it in the database in-place.

## CI/CD Integration

For automated deployments:
1. Store template JSON files in source control
2. Include them in the build/publish process
3. Ensure they are copied to `App_Data/Templates` in the deployment package
4. Application will auto-process them on first startup after deployment

## Airtight Design

- Templates are part of the application package
- No external configuration files needed
- All templates are self-contained within the deployment
- Version control friendly (JSON files in source control)
- Automatic installation and versioning

## Example Files

See the existing template files for examples:
- `retro-sprint-review.json` — Sprint retrospective
- `incident-review.json` — Incident post-mortem
- `ops-pain-points.json` — Operations workshop
- `product-discovery.json` — Product ideation
- `team-building.json` — Team building activities
- `strategic-planning.json` — Strategic planning workshop
- `customer-feedback.json` — Customer discovery session
- `university-lecture.json` — Educational workshop
- `agile-estimation-planning.json` — Agile estimation with Quadrant scoring
- `innovation-brainstorm.json` — Innovation ideation

---

## Validation & Best Practices

### Template Validation Checklist
- [ ] All required fields present (`name`, `description`, `category`, `iconEmoji`, `config`)
- [ ] `category` is a valid enum value
- [ ] `settings.durationMinutes` is realistic for the number of activities
- [ ] Activities have sequential, unique `order` values starting from 1
- [ ] Poll/Quiz activities have at least 2 options with unique `id` and non-empty `label`
- [ ] Quiz activities have `correctOptionIndex` set (zero-based)
- [ ] Quadrant activities have `items` array with at least 1 item and `xScoreOptions` set
- [ ] Quadrant `q1Label`–`q4Label` use meaningful zone names matching the axis context
- [ ] Quadrant `xScoreOptions` entries have `value` parseable as a number
- [ ] Rating `scale` is between 2 and 10
- [ ] GeneralFeedback with `categoriesEnabled: true` has at least 1 category with unique `id`
- [ ] Join form uses `"name": "displayName"` (not `"name": "name"`) when display name should be mandatory
- [ ] `select`/`dropdown`/`multiselect` join form fields have a non-empty `options` array
- [ ] No typos in emoji or descriptions
- [ ] JSON is valid (no syntax errors)

### Activity Design Best Practices
1. **Sequencing**: Start with icebreakers (WordCloud, Poll), build to analysis/discussion activities, close with GeneralFeedback or a closing Poll
2. **Timing**: Allocate 3–5 minutes for quick polls/word clouds, 10–15 minutes for Quadrant and GeneralFeedback, 8–10 minutes for QnA
3. **Breaks**: For sessions over 90 minutes, insert a `Break` activity between major sections
4. **Anonymity**: Enable `allowAnonymous` on sensitive feedback activities (GeneralFeedback), disable for accountability-focused polls
5. **Categories**: Keep `GeneralFeedback` categories to 3–6 with clear, mutually exclusive labels
6. **Quadrant Items**: Define 5–15 items for a Quadrant activity; too many extend the session significantly
7. **Rating Labels**: Always set `minLabel` and `maxLabel` so participants understand the scale meaning
8. **No PRO types**: Do not use `FiveWhys` or `AiSummary` in system templates — these require AI features enabled at the account level
