# TechWayFit Pulse - Activity Types: Complete Reference

> **Purpose:** Comprehensive documentation of all activity types, their participant interactions, configurations, data schemas, and facilitator views.
>
> **Audience:** Product managers, facilitators, developers
>
> **Version:** 1.1 (Last updated: January 2025)
>
> **Implementation Status:** Phase 1 MVP in progress - Poll activity UI complete, MoveNext/GoBack navigation implemented

---

## Table of Contents

1. [Overview](#overview)
2. [Implementation Status](#implementation-status)
3. [Activity Type Summary](#activity-type-summary)
4. [Common Properties](#common-properties)
5. [Activity Type Details](#activity-type-details)
   - [1. Poll](#1-poll)
   - [2. Quiz](#2-quiz)
   - [3. Word Cloud](#3-word-cloud)
   - [4. Q&A](#4-qa)
   - [5. Rating](#5-rating)
   - [6. Quadrant](#6-quadrant)
   - [7. Five Whys](#7-five-whys)
   - [8. General Feedback](#8-general-feedback)
6. [Comparison Matrix](#comparison-matrix)
7. [Implementation Recommendations](#implementation-recommendations)
8. [Facilitator Controls](#facilitator-controls)
9. [Session Flow](#session-flow)

---

## Overview

**TechWayFit Pulse** supports 8 different activity types designed to capture different kinds of participant input during interactive workshops. Each activity type has:

- **Specific interaction pattern** (single-choice, multi-step, visual mapping, etc.)
- **Configuration schema** (JSON stored in `Activity.Config`)
- **Response payload schema** (JSON stored in `Response.Payload`)
- **Dashboard visualization** (how facilitators see aggregated results)

### Design Principles

1. **Mobile-first:** All activities work on phones
2. **Real-time:** Facilitators see results as they come in via SignalR
3. **Filterable:** Results can be sliced by join-form dimensions (team, role, etc.)
4. **Measurable:** Produce quantifiable insights, not just qualitative comments
5. **Sequential pacing:** Facilitators control workshop flow with MoveNext/GoBack
6. **Time-boxed:** Facilitators control when activities open/close

---

## Implementation Status

### ? Completed (Phase 1 - MVP Foundation)

**Infrastructure:**
- ? Clean Architecture structure (Domain, Application, Infrastructure, Contracts, Web)
- ? Entity models: Session, Activity, Participant, Response
- ? EF Core with SQLite (in-memory for development)
- ? SignalR Hub (`WorkshopHub`) for real-time updates
- ? API endpoints for session/activity management
- ? Blazor Server pages with Bootstrap 5.3

**Domain Models:**
- ? `PollConfig` + `PollResponse` - Complete
- ? `RatingConfig` + `RatingResponse` - Complete
- ? `WordCloudConfig` + `WordCloudResponse` - Complete
- ? `GeneralFeedbackConfig` + `GeneralFeedbackResponse` - Complete

**Participant UI:**
- ? `/Pages/Participant/Activity.razor` - Enhanced with all 4 MVP types
- ? Config parsing for Poll and GeneralFeedback
- ? Response submission logic for all types
- ? Real-time SignalR updates

**Facilitator UI:**
- ? `/facilitator/live` - Live session management page
- ? Activity creation page for Poll (`/facilitator/activities/create-poll`)
- ? **MoveNext/GoBack navigation** - Sequential activity flow
- ? Improved layout (QR code sidebar, activities in main area)
- ? "Add Activity" dropdown with type selection
- ? Session code pre-fill from query parameters

**API:**
- ? `CreateActivityRequest` contract
- ? `PulseApiService.CreateActivityAsync()` method
- ? Session creation, participant join, response submission

### ?? In Progress (Blocked by Auth)

**Facilitator Authentication:**
- ?? MoveNext/GoBack buttons exist but require `facilitatorToken`
- ?? Open/Close activity requires `facilitatorToken`
- ?? Need to implement token storage (session/cookie/query)

**Next Steps:**
1. Implement facilitator token storage
2. Wire up Open/Close/MoveNext/GoBack API calls
3. Add response validation services
4. Add response aggregation services
5. Build facilitator dashboards

### ? Not Started

**Validation Layer:**
- ? `IPollResponseValidator` / `PollResponseValidator`
- ? `IRatingResponseValidator` / `RatingResponseValidator`
- ? `IWordCloudResponseValidator` / `WordCloudResponseValidator`
- ? `IGeneralFeedbackResponseValidator` / `GeneralFeedbackResponseValidator`

**Aggregation Layer:**
- ? `IPollAggregationService` / `PollAggregationService`
- ? `IRatingAggregationService` / `RatingAggregationService`
- ? `IWordCloudAggregationService` / `WordCloudAggregationService`
- ? `IGeneralFeedbackAggregationService` / `GeneralFeedbackAggregationService`

**Dashboard UI:**
- ? `PollDashboard.razor`
- ? `RatingDashboard.razor`
- ? `WordCloudDashboard.razor`
- ? `GeneralFeedbackDashboard.razor`

**Activity Types (Phase 2+):**
- ? Quiz
- ? Q&A
- ? Quadrant
- ? Five Whys

---

## Activity Type Summary

| ID | Name | Purpose | Interaction Type | Primary Output |
|----|------|---------|------------------|----------------|
| 0 | Poll | Gauge consensus or identify priorities | Single/Multiple choice | Vote distribution |
| 1 | Quiz | Test knowledge or understanding | Q&A with correct answers | Accuracy scores |
| 2 | Word Cloud | Capture sentiment in keywords | Open text (short) | Word frequency |
| 3 | Q&A | Collect questions for discussion | Open text + voting | Ranked questions |
| 4 | Rating | Numeric feedback on scale | Numeric selection | Average & distribution |
| 5 | Quadrant | 2D prioritization mapping | Point placement (x, y) | Scatter plot clusters |
| 6 | Five Whys | Root cause analysis | Multi-step conversational | Causal chains |
| 7 | General Feedback | Open-ended input | Long-form text | Categorized feedback |

---

## Common Properties

All activities share these base properties (stored in `Activity` entity):

### Activity Base Schema

```json
{
  "id": "GUID",
  "sessionId": "GUID",
  "order": 1,
  "type": "Poll|Quiz|WordCloud|QnA|Rating|Quadrant|FiveWhys|GeneralFeedback",
  "title": "Activity title shown to participants",
  "prompt": "Optional instruction text shown at top",
  "config": "{...activity-specific JSON...}",
  "status": "Pending|Open|Closed",
  "openedAt": "ISO 8601 timestamp",
  "closedAt": "ISO 8601 timestamp"
}
```

### Response Base Schema

All responses share this structure:

```json
{
  "id": "GUID",
  "sessionId": "GUID",
  "activityId": "GUID",
  "participantId": "GUID",
  "payload": "{...activity-specific JSON...}",
  "dimensions": "{...snapshot of participant's join-form data...}",
  "createdAt": "ISO 8601 timestamp"
}
```

The `dimensions` field enables filtering (e.g., "Show me only responses from Team=Ops, Role=Supervisor").

---

## Activity Type Details

---

## 1. Poll

### Purpose
Quickly gauge participant opinions, preferences, or priorities through multiple-choice questions.

### Use Cases
- "Which process step slows you down most?"
- "Which feature should we prioritize?"
- "How familiar are you with this tool?"

### Participant Interaction

**Screen Flow:**
1. See question/prompt
2. View list of options (radio buttons or checkboxes)
3. Select one or multiple options
4. Click "Submit"
5. See confirmation (optional: show live results if configured)

**Interaction Type:** Single interaction (one submission)

### Configuration Schema

```json
{
  "options": [
    {
      "id": "opt-1",
      "label": "Approvals",
      "description": "Waiting for manager sign-off"
    },
    {
      "id": "opt-2",
      "label": "Handoffs",
      "description": "Transferring work between teams"
    },
  {
      "id": "opt-3",
      "label": "Tool friction",
    "description": "Multiple systems that don't integrate"
    }
  ],
  "allowMultiple": false,
  "minSelections": 1,
  "maxSelections": 1,
  "allowCustomOption": true,
  "customOptionPlaceholder": "Other (please specify)",
  "randomizeOrder": false,
  "showResultsAfterSubmit": false
}
```

**Configuration Fields:**
- `options`: Array of choice objects (id, label, optional description)
- `allowMultiple`: If true, use checkboxes; if false, radio buttons
- `minSelections`: Minimum choices required (default: 1)
- `maxSelections`: Maximum choices allowed (only if allowMultiple=true)
- `allowCustomOption`: Add "Other" field for write-in responses
- `randomizeOrder`: Randomize option order to reduce bias
- `showResultsAfterSubmit`: Show participant the current vote distribution

### Response Payload Schema

**Single selection:**
```json
{
  "selectedOptionIds": ["opt-2"]
}
```

**Multiple selections:**
```json
{
  "selectedOptionIds": ["opt-1", "opt-3"]
}
```

**With custom option:**
```json
{
  "selectedOptionIds": ["custom"],
  "customOptionText": "Unclear approval criteria"
}
```

### Dashboard Visualization

**Primary View: Bar Chart**
```
Approvals        ???????????????????? 45% (18 votes)
Handoffs         ???????????? 30% (12 votes)
Tool friction    ???????? 20% (8 votes)
Other  ?? 5% (2 votes)
```

**Secondary Views:**
- Pie chart (percentage breakdown)
- Table with vote counts
- Breakdown by dimensions (e.g., "Ops team chose Handoffs, Tech team chose Tool friction")

**Metrics:**
- Total responses
- Participation rate (responded / total participants)
- Consensus score (% choosing top option)

---

## 2. Quiz

### Purpose
Test participant knowledge or understanding with questions that have correct answers.

### Use Cases
- "What is our current SLA for approval requests?"
- "Which team owns the handoff process?"
- Pre/post training knowledge checks

### Participant Interaction

**Screen Flow:**
1. Read question
2. Select answer from multiple choices
3. Submit
4. See feedback (correct/incorrect) if configured
5. View explanation if provided

**Interaction Type:** Single interaction per question

### Configuration Schema

```json
{
  "questions": [
    {
  "id": "q1",
      "text": "What is our SLA for approval requests?",
      "options": [
        {"id": "a1", "label": "24 hours"},
   {"id": "a2", "label": "48 hours"},
  {"id": "a3", "label": "72 hours"}
      ],
      "correctAnswerId": "a2",
      "explanation": "The SLA was updated to 48 hours in Q3 2023 per policy ABC-123."
    }
  ],
  "showCorrectAnswers": true,
  "showExplanations": true,
  "scoreEnabled": true,
  "passingScore": 70,
  "allowRetry": false
}
```

**Configuration Fields:**
- `questions`: Array of question objects
- `showCorrectAnswers`: Immediately show correct/incorrect after submit
- `showExplanations`: Display explanation text after submit
- `scoreEnabled`: Calculate and show participant score
- `passingScore`: Percentage required to "pass" (if scoreEnabled)
- `allowRetry`: Let participants re-answer incorrect questions

### Response Payload Schema

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

### Dashboard Visualization

**Primary View: Question Accuracy Table**
```
Question        Correct  Incorrect  Accuracy
What is our SLA?                  15       3    83%
Which team owns handoffs?         10     8          56%
```

**Secondary Views:**
- Individual participant scores
- Score distribution (histogram)
- Knowledge gap analysis (lowest scoring questions)
- Breakdown by dimensions (e.g., "Managers scored higher than officers")

**Metrics:**
- Average score (across all participants)
- Pass rate (% scoring above passingScore)
- Question difficulty (% getting each question right)

---

## 3. Word Cloud

### Purpose
Capture open-ended sentiment, themes, or ideas in short keywords or phrases.

### Use Cases
- "Describe the biggest pain point in ONE word"
- "What comes to mind when you think of our approval process?"
- "Name a tool you use daily"

### Participant Interaction

**Screen Flow:**
1. Read prompt
2. Type into text field (1-5 words typically)
3. Submit
4. Optionally submit multiple words (up to configured limit)

**Interaction Type:** Single or multiple short text submissions

### Configuration Schema

```json
{
  "maxWords": 3,
  "minWordLength": 3,
  "maxWordLength": 50,
  "placeholder": "Enter a word or short phrase",
  "allowMultipleSubmissions": true,
  "maxSubmissionsPerParticipant": 3,
  "stopWords": ["the", "and", "is", "a", "an"],
  "caseSensitive": false
}
```

**Configuration Fields:**
- `maxWords`: Maximum words per submission (e.g., 3 = allow "approval sign off")
- `minWordLength`: Minimum characters per word (filter out "a", "is")
- `maxWordLength`: Maximum characters to prevent essays
- `allowMultipleSubmissions`: Let participants submit multiple times
- `maxSubmissionsPerParticipant`: Limit submissions if allowMultiple=true
- `stopWords`: Common words to exclude from cloud
- `caseSensitive`: Treat "Handoff" and "handoff" as different (usually false)

### Response Payload Schema

**Single submission:**
```json
{
  "text": "handoffs"
}
```

**Multi-word submission:**
```json
{
  "text": "approval sign off"
}
```

**Multiple submissions (separate Response records):**
```json
// Response 1
{"text": "handoffs"}

// Response 2
{"text": "rework"}
```

### Dashboard Visualization

**Primary View: Word Cloud (Visual)**
```
       handoffs
  approvals        rework
    WAITING     visibility
      manual    clarity
```
Font size proportional to frequency.

**Secondary Views:**
- Top 20 words table with counts
- Word frequency histogram
- Sentiment analysis (if AI enabled: positive/negative/neutral)
- Breakdown by dimensions

**Metrics:**
- Total unique words
- Total submissions
- Top 5 themes
- Diversity score (unique words / total words)

---

## 4. Q&A

### Purpose
Collect questions from participants for facilitator to address during or after the workshop.

### Use Cases
- "What questions do you have about the new process?"
- "Ask anything about the tool"
- Anonymous Q&A for leadership

### Participant Interaction

**Screen Flow:**
1. Read prompt
2. Type question into text area
3. Submit
4. Optionally upvote other participants' questions
5. See ranked list of all questions

**Interaction Type:** Submit questions + vote on others' questions

### Configuration Schema

```json
{
  "allowUpvoting": true,
  "allowDownvoting": false,
  "allowAnonymous": true,
  "moderationEnabled": true,
  "maxQuestionLength": 500,
  "maxQuestionsPerParticipant": 3,
  "showQuestionCount": true,
  "sortBy": "upvotes|chronological|random"
}
```

**Configuration Fields:**
- `allowUpvoting`: Participants can upvote questions
- `allowDownvoting`: Participants can downvote (rarely used)
- `allowAnonymous`: Hide participant identity on questions
- `moderationEnabled`: Facilitator must approve before showing
- `maxQuestionLength`: Character limit
- `maxQuestionsPerParticipant`: Limit submissions
- `showQuestionCount`: Display total question count to participants
- `sortBy`: Default sorting (usually upvotes)

### Response Payload Schema

**Question submission:**
```json
{
  "type": "question",
  "text": "Will the new process integrate with our existing CRM?",
  "isAnonymous": true
}
```

**Upvote (separate Response record):**
```json
{
  "type": "upvote",
  "targetResponseId": "GUID-of-question-being-upvoted"
}
```

### Dashboard Visualization

**Primary View: Ranked Question List**
```
1. Will the new process integrate with CRM?    ? 12 votes
2. How long will training take?? 8 votes
3. Who approves over $10k?     ? 5 votes
```

**Secondary Views:**
- Question timeline (chronological)
- Unanswered questions (flagged for follow-up)
- Questions by category (if AI-powered tagging enabled)

**Facilitator Actions:**
- Mark as answered
- Pin important questions
- Hide inappropriate questions
- Group related questions

**Metrics:**
- Total questions submitted
- Total upvotes cast
- Average upvotes per question
- Engagement rate (% of participants who submitted or upvoted)

---

## 5. Rating

### Purpose
Capture numeric feedback on a defined scale (satisfaction, confidence, likelihood, etc.).

### Use Cases
- "How satisfied are you with the current approval process? (1-5)"
- "How confident are you in implementing this change? (1-10)"
- "Net Promoter Score: How likely are you to recommend this tool?"

### Participant Interaction

**Screen Flow:**
1. Read question/prompt
2. Select number from scale (buttons, slider, or stars)
3. Optionally add comment
4. Submit

**Interaction Type:** Single numeric selection + optional text

### Configuration Schema

```json
{
  "scale": 5,
  "minLabel": "Very dissatisfied",
  "maxLabel": "Very satisfied",
  "midpointLabel": "Neutral",
  "allowComments": true,
  "commentRequired": false,
  "commentPlaceholder": "Tell us more (optional)",
  "displayType": "buttons|slider|stars",
  "showAverageAfterSubmit": false
}
```

**Configuration Fields:**
- `scale`: Number of points (1-5, 1-10, etc.)
- `minLabel`: Label for lowest value
- `maxLabel`: Label for highest value
- `midpointLabel`: Label for middle value (if odd-numbered scale)
- `allowComments`: Show optional text field
- `commentRequired`: Force participants to explain their rating
- `displayType`: UI presentation (buttons, slider, star icons)
- `showAverageAfterSubmit`: Show current average to participant

### Response Payload Schema

**Rating only:**
```json
{
  "rating": 3,
  "scale": 5
}
```

**With comment:**
```json
{
  "rating": 2,
  "scale": 5,
  "comment": "Too many manual steps"
}
```

### Dashboard Visualization

**Primary View: Distribution Histogram**
```
5 ?????  ???????? 40% (8)
4 ????   ???? 20% (4)
3 ???    ?????? 30% (6)
2 ??     ?? 10% (2)
1 ?      0% (0)

Average: 3.8 / 5.0
```

**Secondary Views:**
- Trend over time (if activity reopened multiple times)
- Comments grouped by rating level
- Net Promoter Score calculation (if scale is 0-10)
- Breakdown by dimensions

**Metrics:**
- Average rating
- Standard deviation (consistency)
- Promoters vs. Detractors (NPS scoring)
- Distribution percentages

---

## 6. Quadrant

### Purpose
Map ideas/issues on a 2D grid for prioritization (Impact vs. Effort, Urgency vs. Importance, etc.).

### Use Cases
- "Plot pain points on Impact vs. Effort"
- "Map features on Value vs. Complexity"
- "Place improvements on Quick Wins vs. Strategic Bets"

### Participant Interaction

**Screen Flow:**
1. Read quadrant axis labels
2. Drag/tap to place point on 2D grid
3. Optionally add label to the point
4. Submit (can place multiple points if configured)

**Interaction Type:** Visual point placement (x, y coordinates)

### Configuration Schema

```json
{
  "xAxis": {
    "label": "Effort",
    "min": 1,
    "max": 5,
    "minLabel": "Low effort",
    "maxLabel": "High effort"
  },
  "yAxis": {
    "label": "Impact",
    "min": 1,
    "max": 5,
    "minLabel": "Low impact",
    "maxLabel": "High impact"
  },
  "quadrantLabels": {
  "topLeft": "Quick Wins",
    "topRight": "Big Bets",
    "bottomLeft": "Fill-ins",
    "bottomRight": "Hard Slogs"
  },
  "allowLabels": true,
  "maxLabelLength": 100,
  "maxPointsPerParticipant": 3,
  "showOthersPoints": false,
  "enableClustering": true
}
```

**Configuration Fields:**
- `xAxis`: Horizontal axis configuration (label, scale, endpoints)
- `yAxis`: Vertical axis configuration
- `quadrantLabels`: Names for the four quadrants
- `allowLabels`: Let participants name their points
- `maxLabelLength`: Limit label text length
- `maxPointsPerParticipant`: How many points one person can place
- `showOthersPoints`: Real-time visibility of other participants' points
- `enableClustering`: Auto-group nearby points on dashboard

### Response Payload Schema

**Single point:**
```json
{
  "x": 2,
  "y": 4,
  "label": "Reduce approval steps"
}
```

**Multiple points (separate Response records):**
```json
// Response 1
{"x": 2, "y": 4, "label": "Reduce approval steps"}

// Response 2
{"x": 4, "y": 3, "label": "Automate handoffs"}
```

### Dashboard Visualization

**Primary View: Scatter Plot**
```
        High Impact
   ?
   Quick    ?    Big
    Wins    ?    Bets
  ?         ?         ?
          ?      ?
????????????????????????????? Effort
            ?
       ?    ?
   Fill-ins ?    Hard
            ?    Slogs
   Low Impact
```

**Secondary Views:**
- Heatmap (density of points)
- Quadrant summary (counts per quadrant)
- Point list table with coordinates
- Cluster analysis (group similar points)

**Facilitator Actions:**
- Merge duplicate points
- Highlight/spotlight specific points
- Label clusters

**Metrics:**
- Total points placed
- Points per quadrant (e.g., 60% in "Quick Wins")
- Average coordinates
- Cluster density

---

## 7. Five Whys

### Purpose
Guide participants through root cause analysis by progressively asking "Why?" to reach underlying systemic issues.

### Use Cases
- "Why do approvals take too long?" ? drill to root cause
- "Why do we have rework?" ? find systemic gap
- "Why is onboarding slow?" ? identify bottleneck

### Participant Interaction

**Screen Flow (Guided Wizard):**
1. See initial problem statement
2. Answer Level 1: "Why does [problem] happen?"
3. System shows answer and asks Level 2: "Why does [your answer] happen?"
4. Continue through levels until:
   - AI detects root cause (recommended)
   - Participant reaches max depth (e.g., Level 5)
   - Participant chooses "Submit early" (after min depth, e.g., Level 3)
5. Review full chain before final submit

**Interaction Type:** Multi-step conversational (3-7 levels)

### Configuration Schema

```json
{
  "initialProblem": "Why do approvals take too long?",
  "context": "We're a financial services company. Approvals involve manager sign-off for transactions over $5000.",
  "targetDepth": 5,
  "minDepth": 3,
  "maxDepth": 7,
  "promptTemplate": "Why does '{previousAnswer}' happen?",
  "allowEarlySubmit": true,
  "showProgressIndicator": true,
  "minAnswerLength": 10,
"maxAnswerLength": 200,
  "aiEnabled": true,
  "aiProvider": "AzureOpenAI|OpenAI|None",
  "stopWhenRootCauseDetected": true
}
```

**Configuration Fields:**
- `initialProblem`: The starting "Why?" question
- `context`: Background information for AI analysis (optional)
- `targetDepth`: Ideal number of levels (usually 5)
- `minDepth`: Minimum levels before allowing submit (usually 3)
- `maxDepth`: Hard stop (usually 7)
- `promptTemplate`: How to phrase subsequent questions
- `allowEarlySubmit`: Let participants stop after minDepth
- `showProgressIndicator`: Display "Step 2 of 5" bar
- `minAnswerLength`: Prevent one-word answers
- `maxAnswerLength`: Prevent essays
- `aiEnabled`: Use AI for dynamic questioning and root cause detection
- `aiProvider`: Which AI service to use
- `stopWhenRootCauseDetected`: AI can stop early if root cause found

### Response Payload Schema

**Without AI:**
```json
{
  "initialProblem": "Why do approvals take too long?",
  "chain": [
    {
   "level": 1,
      "question": "Why do approvals take too long?",
      "answer": "Managers are overloaded"
    },
    {
      "level": 2,
      "question": "Why are managers overloaded?",
      "answer": "No delegation policy exists"
    },
    {
      "level": 3,
   "question": "Why doesn't a delegation policy exist?",
      "answer": "Unclear risk thresholds"
    },
    {
      "level": 4,
      "question": "Why are risk thresholds unclear?",
      "answer": "Leadership hasn't defined risk appetite"
    },
    {
"level": 5,
      "question": "Why hasn't leadership defined risk appetite?",
      "answer": "No regulatory pressure yet"
 }
  ],
  "depthReached": 5,
  "completedAt": "2024-01-15T10:30:00Z"
}
```

**With AI Metadata:**
```json
{
  "initialProblem": "Why do approvals take too long?",
  "context": "Financial services, $5000 threshold",
  "chain": [
    {
      "level": 1,
      "question": "Why do approvals take too long?",
    "answer": "Managers are overloaded",
"aiMetadata": {
        "answerType": "symptom",
        "depthScore": 0.2,
     "isRootCause": false,
        "category": "workload"
      }
    },
    {
      "level": 2,
      "question": "Why are managers overloaded?",
      "answer": "No delegation policy exists",
      "aiMetadata": {
        "answerType": "organizational_constraint",
        "depthScore": 0.6,
        "isRootCause": false,
        "category": "governance"
      }
    },
    {
"level": 3,
      "question": "Why hasn't a delegation policy been created?",
      "answer": "Leadership hasn't defined risk appetite thresholds",
 "aiMetadata": {
 "answerType": "root_cause",
        "depthScore": 0.92,
        "isRootCause": true,
  "category": "governance",
        "systemicLevel": "high"
      }
    }
  ],
  "depthReached": 3,
  "stoppedEarly": true,
  "stoppingReason": "AI detected root cause at Level 3",
  "aiSummary": {
    "rootCause": "Leadership hasn't defined risk appetite thresholds",
    "category": "Governance & Decision Rights",
    "systemicLevel": "high",
"actionableInsight": "Establish clear risk appetite framework with delegation thresholds",
    "confidence": 0.92
  },
  "completedAt": "2024-01-15T10:30:00Z"
}
```

### Dashboard Visualization

**Primary View: Tree/Ladder View**
```
Participant 1 (Depth: 5)
  L1: Managers are overloaded
    L2: No delegation policy exists
      L3: Unclear risk thresholds
 L4: Leadership hasn't defined risk appetite
   L5: No regulatory pressure yet
  [Root Cause: Governance]

Participant 2 (Depth: 4)
  L1: Too many approval steps
    L2: Historical process from merger
   L3: Systems don't integrate
     L4: No budget for integration
        [Root Cause: Technology/Budget]
```

**Secondary Views:**
- Root Cause Clustering:
  ```
  Governance Issues (8 participants)
  Technology Gaps (5 participants)
  Process Complexity (3 participants)
  ```
- Depth Metrics:
  ```
  Average depth: 4.2 levels
  Completed to Level 5: 12/20 (60%)
  Stopped at Level 3: 4/20 (20%)
  ```
- AI Theme Analysis (if enabled):
  ```
  Common themes across levels:
  - Risk management (mentioned 12 times at L3-L5)
  - Legacy systems (mentioned 8 times at L2-L4)
  - Organizational structure (mentioned 6 times at L1-L3)
  ```

**Metrics:**
- Total chains completed
- Average depth reached
- Most common root causes
- Systemicity score (how many chains reach fundamental issues)

---

## 8. General Feedback

### Purpose
Collect open-ended feedback, suggestions, or problem descriptions without constraints.

### Use Cases
- "Share any thoughts about the workshop"
- "Describe any problems you're experiencing"
- "What suggestions do you have?"
- End-of-session feedback

### Participant Interaction

**Screen Flow:**
1. Read prompt
2. Type into large text area
3. Optionally select category (if configured)
4. Submit

**Interaction Type:** Single long-form text submission

### Configuration Schema

```json
{
  "maxLength": 1000,
  "minLength": 10,
  "placeholder": "Share your thoughts, problems, or suggestions...",
  "allowAnonymous": true,
  "categoriesEnabled": true,
  "categories": [
    {"id": "problem", "label": "Problem", "icon": "??"},
 {"id": "suggestion", "label": "Suggestion", "icon": "??"},
    {"id": "question", "label": "Question", "icon": "?"}
  ],
  "requireCategory": false,
  "allowAttachments": false,
  "showCharacterCount": true
}
```

**Configuration Fields:**
- `maxLength`: Maximum characters (prevent novels)
- `minLength`: Minimum characters (prevent empty submissions)
- `placeholder`: Hint text in text area
- `allowAnonymous`: Hide participant identity
- `categoriesEnabled`: Show category selector
- `categories`: Predefined categories with labels/icons
- `requireCategory`: Force participant to choose category
- `allowAttachments`: Future feature (file uploads)
- `showCharacterCount`: Display remaining characters

### Response Payload Schema

**Basic feedback:**
```json
{
  "text": "We need better documentation for the approval process. Currently, everyone follows different rules, which causes confusion and delays.",
  "characterCount": 142
}
```

**With category:**
```json
{
  "text": "We need better documentation for the approval process.",
  "category": "problem",
  "characterCount": 56
}
```

**Anonymous:**
```json
{
  "text": "I'm concerned about the timeline for implementation.",
  "category": "question",
  "isAnonymous": true,
  "characterCount": 55
}
```

### Dashboard Visualization

**Primary View: Categorized List**
```
Problems (8)
?? "We need better documentation for approvals"
?? "Too many manual steps in handoff process"
?? "Unclear who to escalate to"

Suggestions (5)
?? "Create a single source of truth for policies"
?? "Automate email notifications"
?? "Set up monthly training sessions"

Questions (3)
Praise (2)
```

**Secondary Views:**
- Full text view (paginated or scrollable)
- Word cloud from all feedback text
- Sentiment analysis (if AI enabled: positive/negative/neutral)
- Tag extraction (if AI enabled: auto-tag common themes)

**Facilitator Actions:**
- Flag for follow-up
- Mark as addressed
- Tag with custom labels
- Export to CSV/PDF

**Metrics:**
- Total feedback submissions
- Average length
- Feedback by category (if enabled)
- Sentiment distribution (if AI enabled)

---

## Comparison Matrix

| Feature | Poll | Quiz | Word Cloud | Q&A | Rating | Quadrant | Five Whys | General Feedback |
|---------|------|------|------------|-----|--------|----------|-----------|------------------|
| **Submission Type** | Single choice | Q&A | Short text | Long text + votes | Numeric | Visual (x,y) | Multi-step | Long text |
| **Multiple Submissions** | No | No | Optional | Multiple | No | Optional | No | No |
| **Real-time Visibility** | Yes | Yes | Yes | Yes | Yes | Optional | Partial | Yes |
| **AI Enhancement** | Low | Low | Medium | High | Low | Medium | **High** | High |
| **Analysis Complexity** | Low | Low | Medium | Medium | Low | High | **Very High** | Medium |
| **Mobile Friendly** | ? High | ? High | ? High | ? High | ? High | ?? Medium | ? High | ? High |
| **Time to Complete** | 30s | 1-2 min | 30s | 1-2 min | 20s | 1-3 min | 3-5 min | 1-3 min |
| **Facilitator Effort** | Low | Medium | Low | Medium | Low | Medium | Low | Medium |
| **Output Measurability** | High | Very High | Medium | Low | Very High | High | Very High | Low |

---

## Implementation Recommendations

### Phase 1: MVP (Launch Day) ? In Progress
**Implement these first:**
1. ? **Poll** - Complete (UI + domain models)
2. ?? **Rating** - Domain models complete, UI pending
3. ?? **Word Cloud** - Domain models complete, UI pending
4. ?? **General Feedback** - Domain models complete, UI pending

**Status:** Poll creation UI complete, MoveNext/GoBack navigation implemented, blocked by facilitator authentication.

**Remaining Tasks:**
- [ ] Implement facilitator token storage
- [ ] Wire up Open/Close/MoveNext/GoBack API calls
- [ ] Build response validation services
- [ ] Build response aggregation services
- [ ] Build facilitator dashboards (Poll, Rating, WordCloud, GeneralFeedback)
- [ ] Create UI for Rating, Word Cloud, General Feedback activities

**Why:** Cover 80% of use cases with lowest complexity.

### Phase 2: Interactive Features
**Add next:**
5. **Q&A** - Requires upvoting logic and moderation UI
6. **Quadrant** - Requires 2D visualization and clustering

**Why:** More complex interactions but high workshop value.

### Phase 3: Advanced Analytics
**Add last:**
7. **Quiz** - Requires answer validation and scoring logic
8. **Five Whys** - Requires multi-step UX and ideally AI integration

**Why:** Most complex, but powerful for root cause workshops.

---

### Current Architecture

**Project Structure:**
```
src/
??? TechWayFit.Pulse.Domain/
?   ??? Entities/ (Session, Activity, Participant, Response)
?   ??? Models/
?   ?   ??? ActivityConfigs/ ? (Poll, Rating, WordCloud, GeneralFeedback)
?   ?   ??? ResponsePayloads/ ? (Poll, Rating, WordCloud, GeneralFeedback)
?   ??? Enums/ (ActivityType, ActivityStatus, SessionStatus)
??? TechWayFit.Pulse.Application/
?   ??? Abstractions/
?   ?   ??? Repositories/
?   ?   ??? Services/ ?? (Validation and Aggregation pending)
?   ??? Services/ ?? (Implementations pending)
??? TechWayFit.Pulse.Infrastructure/
?   ??? Persistence/ ? (EF Core, SQLite)
?   ??? Repositories/ ? (SessionRepository, etc.)
??? TechWayFit.Pulse.Contracts/
?   ??? Requests/ ? (CreateActivityRequest, etc.)
?   ??? Responses/ ? (ActivityResponse, etc.)
??? TechWayFit.Pulse.Web/
    ??? Controllers/ ? (API controllers)
    ??? Pages/
    ?   ??? Facilitator/
    ?   ?   ??? Live.razor ? (MoveNext/GoBack complete)
    ?   ?   ??? CreatePollActivity.razor ?
    ?   ??? Participant/
    ?       ??? Activity.razor ? (All 4 types)
    ??? Services/ ? (PulseApiService)
    ??? Hubs/ ? (WorkshopHub for SignalR)
```

---

### AI Integration Priority

**High Priority (Immediate Value):**
- **Five Whys:** AI-powered root cause detection and dynamic questioning
- **General Feedback:** AI-powered categorization and theme extraction
- **Q&A:** AI-powered question grouping and topic tagging

**Medium Priority (Nice to Have):**
- **Word Cloud:** Sentiment analysis and synonym grouping
- **Quadrant:** Cluster naming and insight generation

**Low Priority (Future):**
- **Poll:** Auto-suggest options based on context
- **Rating:** Predictive analysis based on comments

---

### Technical Considerations

**Database Storage:**
- All activity configs: Store as JSON text in `Activity.Config`
- All responses: Store as JSON text in `Response.Payload`
- Enable filtering: Store participant dimensions in `Response.Dimensions`
- **Implementation:** ? Complete (EF Core with SQLite)

**API Design:**
- Single endpoint: `POST /api/sessions/{code}/activities/{activityId}/responses`
- Payload validation based on `Activity.Type`
- Rate limiting per participant (prevent spam)
- **Implementation:** ? Complete, validation layer pending

**Real-time Updates:**
- SignalR event: `ResponseReceived` ? Update dashboard counters
- SignalR event: `DashboardUpdated` ? Refresh charts/visualizations
- Throttle updates (e.g., batch every 2 seconds to prevent UI flicker)
- **Implementation:** ? SignalR Hub complete, dashboard updates pending

**Mobile Optimization:**
- Quadrant: Use sliders on mobile instead of drag-and-drop
- Five Whys: One question per screen (wizard style)
- Word Cloud: Auto-suggest common words to speed input
- All: Large touch targets (min 44x44px buttons)
- **Implementation:** ? Bootstrap 5.3 provides mobile-first styling

**Authentication & Authorization:**
- **Current Issue:** ?? Facilitator actions require authentication
- **Options:**
  1. Store `facilitatorToken` in session storage after join
  2. Pass via query parameter (simple, less secure)
  3. Use HTTP-only cookies (most secure)
- **Implementation:** ? Not started (blocking MoveNext/GoBack wire-up)

---

## Appendix: Recent Changes

### Version 1.1 Updates (January 2025)

**Major Changes:**
1. **MoveNext/GoBack Navigation** - Implemented sequential activity flow
- Current activity highlighted at top of Live page
   - Go Back, Close, Next Activity buttons
   - Context-sensitive activity card controls
   - Enforces strict sequential mode (only one activity open)

2. **Improved Live Page Layout**
   - Moved activities from cramped sidebar to main content area
   - Activities display as cards with order badges and status
   - QR code + session info in left sidebar (35%)
   - Main content area (65%) shows current activity + all activities
   - Scales better for 10+ activities (scrollable)

3. **Poll Activity Creation Flow**
   - New page: `/facilitator/activities/create-poll`
   - Session code pre-fill from query parameter
   - Dynamic option management (add/remove)
   - Settings: multiple selections, custom option, min/max selections
   - "Go to Console" button returns to Live page

4. **Activity Type Dropdown**
   - "Add Activity" dropdown on Live page
   - Shows all 8 activity types (4 active, 4 coming soon)
   - Auto-passes session code to creation pages

5. **Domain Models Complete**
   - All 4 Phase 1 activity types have config + response models
   - Strongly-typed, documented classes
   - Ready for validation and aggregation services

**Bug Fixes:**
- ? Fixed EF Core entity tracking conflict in `SessionRepository.UpdateAsync`
- ? Fixed HttpClient BaseAddress issue in Blazor Server
- ? Fixed HTML structure in Live.razor (missing closing divs)
- ? Removed standalone HTML comments confusing Razor parser

**Documentation:**
- ? Created implementation roadmap
- ? Created testing guide
- ? Created MoveNext/GoBack implementation guide
- ? Documented all bugs and fixes

---

## Document Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024-01-15 | Initial comprehensive documentation |
| 1.1 | 2025-01-XX | Added implementation status, facilitator controls, session flow, architecture updates, recent changes |

---

**End of Document**
