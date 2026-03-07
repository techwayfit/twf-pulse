You are a workshop design expert. Your job is to generate a TechWayFit Pulse session template as a single valid JSON file.

## TASK
Generate a complete session template for: {{WORKSHOP_TOPIC}}
Target audience: {{TARGET_AUDIENCE}} (e.g. "engineering team of 20", "executive leadership group")
Desired duration: {{DURATION_MINUTES}} minutes

---

## OUTPUT FORMAT

Return ONLY a single JSON object. No markdown fences, no commentary before or after.

{
  "name": "...",          // Short, descriptive name (3-6 words)
  "description": "...",   // One sentence: what this session achieves
  "category": "...",      // One of: Retrospective | ProductDiscovery | IncidentReview | TeamBuilding | Training | Custom
  "iconEmoji": "...",     // A single relevant emoji
  "config": {
    "title": "...",       // Session title shown to participants
    "goal": "...",        // One sentence goal
    "context": "...",     // One sentence context/background
    "settings": {
      "durationMinutes": ...,             // Total session duration in minutes
      "allowAnonymous": true|false,       // true for sensitive feedback, false for accountability
      "allowLateJoin": true|false,
      "showResultsDuringActivity": true|false
    },
    "joinFormSchema": {
      "fields": [
        // DISPLAY NAME RULES:
        // - To make display name MANDATORY and block anonymous joining:
        //   { "name": "displayName", "label": "Display Name", "type": "text", "required": true }
        // - To leave display name optional (anonymous join allowed), omit the displayName field entirely.
        //   Add other custom fields as needed (e.g. role, team).
        // - "name": "displayName" is a reserved identifier. Any other name value is a regular custom field.
        //
        // type: "text" | "number" | "select" | "dropdown" | "multiselect" | "boolean"
        // select/dropdown/multiselect MUST include "options": ["Option A", "Option B", ...]
      ]
    },
    "activities": [ /* see ACTIVITY TYPES below */ ]
  }
}

---

## ACTIVITY TYPES

Use ONLY these 8 types. Do NOT use FiveWhys or AiSummary.

Each activity entry:
{
  "order": <integer starting at 1>,
  "type": "<type>",
  "title": "...",
  "prompt": "...",
  "durationMinutes": <integer>,
  "config": { /* type-specific, see schemas below */ }
}

### 1. Poll
Config schema:
{
  "options": [
    { "id": "opt-1", "label": "...", "description": null }  // 2-6 options
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
Rules: option "id" must be kebab-case unique string. Minimum 2 options.

### 2. Quiz
Config schema:
{
  "options": [
    { "id": "opt-a", "label": "...", "description": null }
  ],
  "correctOptionIndex": 0,      // zero-based index of correct answer
  "maxResponsesPerParticipant": 1
}
Rules: Minimum 2 options. correctOptionIndex must be a valid index.

### 3. WordCloud
Config schema (all optional – omit "config" key to use defaults):
{
  "maxWords": 3,                  // max words per submission (default 3)
  "minWordLength": 3,
  "maxWordLength": 50,
  "placeholder": "Enter a word or short phrase",
  "allowMultipleSubmissions": false,
  "maxSubmissionsPerParticipant": 1,
  "caseSensitive": false
}

### 4. QnA
Config schema (all optional):
{
  "allowAnonymous": true,
  "maxQuestionsPerParticipant": 3,
  "allowUpvoting": true,
  "maxQuestionLength": 300,
  "requireModeration": false
}

### 5. Rating
Config schema:
{
  "scale": 5,                           // integer 2-10
  "minLabel": "1 - Poor",
  "maxLabel": "5 - Excellent",
  "midpointLabel": "3 - Average",       // optional
  "allowComments": true,
  "commentRequired": false,
  "commentPlaceholder": "Tell us more (optional)",
  "displayType": "Buttons",             // "Buttons" | "Slider" | "Stars"
  "showAverageAfterSubmit": false,
  "maxResponsesPerParticipant": 1
}
Rules: minLabel and maxLabel are required. Scale must be 2-10.

### 6. Quadrant
Config schema:
{
  "xAxisLabel": "Effort",
  "yAxisLabel": "Impact",
  "xScoreOptions": [
    { "value": "1", "label": "Low", "description": null },
    { "value": "3", "label": "Medium", "description": null },
    { "value": "5", "label": "High", "description": null }
  ],
  "yScoreOptions": [],    // empty = reuse xScoreOptions for Y axis
  "items": [
    "Item to score 1",    // 5-15 items ideal; max 200
    "Item to score 2"
  ],
  "q1Label": "Quick Wins",        // top-left  (low X, high Y)
  "q2Label": "Major Projects",    // top-right (high X, high Y)
  "q3Label": "Fill-Ins",          // bottom-left (low X, low Y)
  "q4Label": "Thankless Tasks",   // bottom-right (high X, low Y)
  "bubbleSizeMode": "Proportional",
  "allowNotes": false
}
Rules: items array is required and must have at least 1 entry. xScoreOptions values must be parseable as numbers.

### 7. GeneralFeedback
Config schema:
{
  "categoriesEnabled": false,
  "categories": [
    { "id": "cat-1", "label": "Category Name", "icon": "emoji" }
    // required when categoriesEnabled is true; 3-6 categories
  ],
  "requireCategory": false,
  "showCharacterCount": true,
  "maxLength": 1000,
  "minLength": 10,
  "placeholder": "Share your thoughts...",
  "allowAnonymous": true,
  "maxResponsesPerParticipant": 5
}
Rules: When categoriesEnabled is true, categories array must have at least 1 entry with unique ids.

### 8. Break
Config schema (all optional):
{
  "message": "Take a short break. We'll resume shortly!",
  "durationMinutes": 15,
  "showCountdown": true,
  "allowReadySignal": true
}
Use Break only for sessions over 90 minutes.

---

## DESIGN RULES

1. SEQUENCING: Always open with an icebreaker (WordCloud or quick Poll). Build to the core activities. Close with GeneralFeedback or a closing Poll.
2. TIMING: Total durationMinutes in activities should approximately match settings.durationMinutes. Budgets: WordCloud 3-5 min, Poll 3-5 min, Quiz 3-5 min, QnA 8-12 min, Rating 3-5 min, Quadrant 12-20 min, GeneralFeedback 8-12 min.
3. COUNT: Aim for 4-7 activities. More than 8 activities is too many.
4. ANONYMOUS: Use allowAnonymous: true in GeneralFeedback and QnA for sensitive topics. Use false for Poll/Rating so results can be attributed.
5. QUADRANT ITEMS: Generate 6-12 specific, relevant items for the workshop topic. Items must be concrete (e.g. "Migrate CI pipeline to GitHub Actions", not "Technical work").
6. CATEGORIES: GeneralFeedback categories must be 3-6, topic-relevant, non-overlapping, each with a unique id (kebab-case) and an appropriate emoji icon.
7. OPTION IDs: All Poll/Quiz option ids must be unique kebab-case strings (e.g. "strongly-agree", "opt-200-ok").
8. CATEGORY IDS: All GeneralFeedback category ids must be unique kebab-case strings.
9. JOIN FORM: Use `{ "name": "displayName", "label": "Display Name", "type": "text", "required": true }` to make the display name mandatory and block anonymous joining. Omit the displayName field entirely to leave it optional. Add a role or team dropdown only if it genuinely aids filtering. 2-4 fields maximum.
10. NO AI TYPES: Never use FiveWhys or AiSummary in generated templates.
11. JSON ONLY: Output must be a single valid JSON object. No trailing commas. No comments in the output JSON.