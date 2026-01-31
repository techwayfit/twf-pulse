# AI Integration — TechWayFit Pulse

This document explains how AI features are integrated into TechWayFit Pulse, how data flows, how to configure and operate AI features, security and privacy considerations, and extension points for engineers.

**Location**
- Implementation: `src/TechWayFit.Pulse.AI/` (real + mock services)
- Conditional DI registration: `src/TechWayFit.Pulse.Web/Program.cs`
- Controller hook: `src/TechWayFit.Pulse.Web/Controllers/Api/SessionsController.cs`
- Sample config: `publish/appsettings.ai.sample.json`

## Goals
- Provide optional AI-driven features for session generation, participant response analysis, and facilitator prompt generation.
- Keep AI optional: the app works without AI (mock services used when disabled or key missing).
- Ensure privacy (PII sanitization), configurable retention, and explicit opt-in via configuration.

## High-level Components
- `TechWayFit.Pulse.AI` project
  - `ParticipantAIService` — calls OpenAI to analyze participant responses
  - `FacilitatorAIService` — calls OpenAI to generate facilitator prompts
  - `MockParticipantAIService` / `MockFacilitatorAIService` — lightweight fallbacks used when AI disabled
- Web integration
  - DI registration in `Program.cs` (conditional on `AI:Enabled` and `AI:OpenAI:ApiKey`)
  - API controller (`SessionsController`) triggers AI after activity close and broadcasts AI insights over SignalR
- SignalR
  - AI insights are emitted via existing real-time mechanisms (example uses `DashboardUpdated` with `AggregateType = "AIInsight"`)

## Data Flow (Activity close / AI analysis example)
1. Facilitator closes an activity via `POST /api/sessions/{code}/activities/{activityId}/close`.
2. Controller closes the activity, updates session state, and publishes activity state change via SignalR.
3. Controller calls:
   - `_participantAI.AnalyzeParticipantResponsesAsync(sessionId, activityId)`
   - `_facilitatorAI.GenerateFacilitatorPromptAsync(sessionId, activityId)`
   These calls are awaited in-line (current implementation) and return JSON strings.
4. Controller packages analysis and facilitator prompt into a payload and broadcasts it to the facilitator group using `DashboardUpdatedEvent` with `AggregateType = "AIInsight"` and `Payload` containing analysis + prompt.

Notes:
- Current implementation performs AI calls synchronously during the close request. This is simple and immediate, but may add latency. The recommended production approach is to queue analysis to a background worker and notify clients when results are ready.

## Prompting and Output
- Services send simple system/user messages to the AI model instructing it to return JSON payloads (themes, summaries, facilitator prompts).
- The app expects JSON strings; implementations currently return raw AI responses as strings and broadcast them as-is. Consumers should parse and validate before using.
- System prompts and templates are documented in `docs/ai/ai-prompts` (system prompt file used for session generation).

## Configuration
Add AI settings to `appsettings.local.json` or environment variables. Example (see `publish/appsettings.ai.sample.json`):

{
  "AI": {
    "Enabled": true,
    "OpenAI": {
      "ApiKey": "YOUR_OPENAI_API_KEY",
      "Endpoint": "https://api.openai.com/v1/",
      "Model": "gpt-4",
      "TimeoutSeconds": 60
    }
  }
}

Key behavior:
- `AI:Enabled` (bool): Enables AI features. If `false`, the mock services are used.
- `AI:OpenAI:ApiKey` (string): Required for real AI calls. If missing, the app falls back to mocks.
- `AI:OpenAI:Endpoint` (string): Base URL; public OpenAI or Azure OpenAI endpoints are supported.

## Security & Privacy
- Do not commit API keys to source control; use `appsettings.local.json`, environment variables, or secret stores.
- Sanitize context documents before sending to AI (remove emails, phone numbers, PII, and sensitive financials). The design docs recommend a 500-char summary limit per context doc.
- AI data retention: store AI analysis results only as needed. Design recommends 30-day retention for debugging; implement deletion endpoints if required.
- Consent: show a prominent warning when the facilitator provides context documents, and require confirmation that no sensitive data is included.

## Cost & Rate Limiting
- AI calls consume tokens. The design documents include cost estimates and suggest cost-saving tactics:
  - Use GPT-3.5 for cheaper analyses where acceptable
  - Threshold-based analysis (only run if enough responses)
  - Caching and summarization
  - Batching analyses or running in background at intervals
- Add rate limiting and quotas to prevent runaway costs (service-level or per-organization limits).

## SignalR Integration
- AI insights are broadcast with an event shape consistent with `DashboardUpdatedEvent`:
  - `SessionCode` (string)
  - `ActivityId` (Guid)
  - `AggregateType` = `AIInsight`
  - `Payload` = { analysis: <string|JSON>, facilitatorPrompt: <string|JSON> }
  - `Timestamp`
- Facilitator UI should listen for `DashboardUpdated` events with `AggregateType == "AIInsight"` and render the insight panel.

## Extension Points
- Background processing: replace inline AI calls in `SessionsController` with queued tasks and a `BackgroundService` worker to run analyses and push results.
- Richer schema: instead of raw strings, define `AIAnalysisResult` and `AIActivityRecommendation` DTOs in `TechWayFit.Pulse.Contracts` to validate AI outputs.
- Prompt engineering: move system/user prompt templates to `src/TechWayFit.Pulse.Application/AI/Prompts` and load them via a `PromptBuilder` service.
- Model provider abstraction: add `IAICompletionService` to support multiple providers (Azure, OpenAI, on-prem hosted models).

## Testing
- Mock services (`MockParticipantAIService`, `MockFacilitatorAIService`) make it easy to run tests and local development without an API key.
- Add unit tests that assert the controller broadcasts `DashboardUpdated` with `AggregateType = "AIInsight"` when an activity is closed (using mocks).

## Monitoring & Observability
- Log AI request/response metadata (token usage, latency) without sensitive payloads.
- Track metrics: analyses per session, average analysis latency, acceptance rate of AI recommendations, API errors, and cost.

## Deployment & Scaling
- Consider the following when enabling AI in production:
  - Use background processing for scale and latency control
  - Use connection pooling and proper HttpClient registration (already registered as named client `openai`)
  - Add resilient retries with exponential backoff for AI HTTP calls
  - Enforce per-tenant or per-org quotas

## Sample Implementation Notes
- DI: `Program.cs` conditionally registers `TechWayFit.Pulse.AI.Services.ParticipantAIService` and `FacilitatorAIService` when `AI:Enabled` and API key present; otherwise it registers mock implementations.
- Controller: `SessionsController.CloseActivity` triggers AI analysis and broadcasts the result as a `DashboardUpdatedEvent` with `AggregateType = "AIInsight"`.

## AI Session Create Scenario

This scenario describes the **end-to-end flow for generating a workshop session agenda with AI**, including enhanced context inputs (participant types, sprint backlogs, incident reports, product documentation), reviewing/editing the generated activities, and persisting the session.

**Status**: ✅ **Fully Implemented** (January 2026)  
**Documentation**: See [AI-SESSION-GENERATION-IMPLEMENTATION-COMPLETE.md](./AI-SESSION-GENERATION-IMPLEMENTATION-COMPLETE.md) for detailed implementation guide.

---

### What It Does

The AI Session Generation feature allows facilitators to **automatically generate a complete workshop agenda** (3-7 activities) from:

1. **Basic inputs**: Session title, goal, workshop type
2. **Enhanced context** (optional):
   - **Duration & participant count** → AI calculates optimal activity count (30 min = 3 activities, 120 min = 6 activities)
   - **Participant type** (Technical/Business/Managers/Leaders/Mixed) → AI adjusts terminology and focus
   - **Sprint backlog** → AI references specific JIRA tickets or story names in questions
   - **Incident report** (with severity P0-P4) → AI generates postmortem-style activities
   - **Product documentation** → AI references specific features in feedback questions
   - **Temperature control** (0.0-1.0) → Controls AI creativity vs focus

**Example transformation**:

| Input | AI Output |
|-------|-----------|
| Title: "Sprint 24 Retro"<br>Participant Type: Technical<br>Sprint Backlog: "JIRA-123: Stripe Integration ✓, JIRA-456: Rate Limiting (blocked)" | **Activity 1**: "What technical challenges did you face during the Stripe Integration (JIRA-123)?" (WordCloud)<br>**Activity 2**: "What blocked JIRA-456 (Rate Limiting)?" (Poll)<br>**Activity 3**: "How satisfied are you with our code quality this sprint?" (Rating) |

The feature is **backward compatible** — facilitators can still use simple title + goal + workshop type and get generated activities. Enhanced context is **opt-in**.

---

### UX Control Flow

#### Phase 1: Input & Configuration

**Page**: `/facilitator/create-workshop` (CreateWorkshop.razor)

1. **Facilitator fills basic fields** (always visible):
   - Session Title (required)
   - Goal (required)
   - Workshop Type (dropdown: Retro, Discovery, Incident Review, etc.)

2. **Facilitator expands "Show Enhanced Options"** (optional):
   - Sets **Duration** (15-600 minutes) with slider
   - Sets **Expected Participants** (1-1000)
   - Selects **Participant Type**: Technical, Business, Managers, Leaders, or Mixed
     - AI hint appears: "Use technical terminology, focus on implementation details"
   
3. **Facilitator adds context documents** (optional, expandable sections):
   
   **📋 Sprint Backlog**:
   - Checkbox: "Include Sprint Backlog Context"
   - Text area: Sprint summary (max 500 chars) with live counter
   - Text area: Key backlog items (one per line)
   
   **🚨 Incident Report**:
   - Checkbox: "Include Incident Context"
   - Text area: Incident summary (max 500 chars)
   - Dropdown: Severity (P0-P4)
   - Number input: Duration in minutes
   - Text input: Impacted systems (comma-separated)
   
   **📖 Product Documentation**:
   - Checkbox: "Include Product Context"
   - Text area: Product summary (max 500 chars)
   - Text area: Key features (one per line)
   
4. **Facilitator adjusts AI settings** (optional):
   - **Temperature slider** (0.0 = focused, 1.0 = creative)
   - Default: 0.7

5. **Facilitator clicks "Generate with AI"**
   - ⚠️ PII warning visible: "Do not include customer names, emails, or sensitive data"

#### Phase 2: Generation & Review

6. **Loading state**:
   - Button shows spinner: "🔄 Generating..."
   - Button disabled to prevent duplicate requests

7. **Validation** (client-side, before API call):
   - Title & Goal required → Error: "❌ Session title is required"
   - Duration 15-600 → Error: "❌ Duration must be between 15-600 minutes"
   - Summaries ≤500 chars → Error: "❌ Sprint backlog summary must be 500 characters or less"
   - If validation fails, show error inline and stop

8. **API call** (if validation passes):
   - POST to `/api/sessions/generate` with `CreateSessionRequest`
   - Request includes `GenerationContext` (if enhanced options used) or legacy `Context` string

9. **Response handling**:
   - **Success**: Activities list populated (3-7 items)
     - Success message: "✓ Generated 5 activities"
     - Activities displayed with title, prompt, duration, type
     - Each activity has **Edit** and **Remove** buttons
   
   - **Failure**: Error message displayed
     - "❌ Failed to generate: {error message}"
     - Facilitator can retry

10. **Review & edit** (draft mode, client-side only):
    - Click **Edit** → Modal opens with:
      - Title (text input)
      - Prompt (textarea)
      - Duration (number input)
      - Config (JSON textarea for advanced users)
      - Save/Cancel buttons
    - Click **Remove** → Activity removed from list
    - Activities **not persisted** until "Save & Go Live"

#### Phase 3: Finalize & Launch

11. **Facilitator clicks "Save & Go Live"**:
    - Creates session: `POST /api/sessions` with session metadata
    - Server returns session `Code` (e.g., "ABC123")
    - Client obtains facilitator token via `IClientTokenService`
    - Client creates each activity: `POST /api/sessions/{code}/activities` (with token)
    - Navigation: Redirects to `/facilitator/console/{code}` (live session view)

**States & Edge Cases**:
- **No API key**: Falls back to `MockSessionAIService` (generates generic activities)
- **OpenAI error**: Falls back to mock, logs error, shows user-friendly message
- **Empty activities**: Allow manual activity creation
- **Duplicate generation**: `isGenerating` flag prevents concurrent requests

---

### Technical Control Flow

#### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Facilitator Browser (Blazor)                    │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ CreateWorkshop.razor                                          │  │
│  │ - Form inputs (title, goal, duration, participant type)      │  │
│  │ - Context document inputs (sprint, incident, product)        │  │
│  │ - Validation logic                                           │  │
│  │ - Draft activity list (edit/remove)                          │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              ↓ (user clicks "Generate")              │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ PulseApiService.GenerateSessionActivitiesAsync()             │  │
│  │ - Builds CreateSessionRequest with GenerationContext         │  │
│  │ - POST /api/sessions/generate                                │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓ HTTP
┌─────────────────────────────────────────────────────────────────────┐
│                      Backend (ASP.NET Core)                          │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ SessionsController.GenerateSessionActivities()               │  │
│  │ - Receives CreateSessionRequest                              │  │
│  │ - Calls ISessionAIService.GenerateSessionActivitiesAsync()   │  │
│  │ - Returns IReadOnlyList<AgendaActivityResponse>              │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              ↓                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ SessionAIService (TechWayFit.Pulse.AI)                       │  │
│  │ 1. BuildUserPrompt(request)                                  │  │
│  │    - Checks if GenerationContext exists                      │  │
│  │    - If yes: Build enhanced prompt with:                     │  │
│  │      • Participant type intelligence                         │  │
│  │      • Duration → activity count (CalculateActivityCount)    │  │
│  │      • Context documents (BuildContextDocuments)             │  │
│  │      • PII sanitization on all summaries (PiiSanitizer)      │  │
│  │    - If no: Use legacy Context string                        │  │
│  │ 2. BuildSystemPrompt()                                       │  │
│  │    - Returns schema for activity types (Poll, WordCloud...)  │  │
│  │ 3. Calls OpenAI API                                          │  │
│  │    - Model: config["AI:OpenAI:Model"] ?? "gpt-4o-mini"       │  │
│  │    - Temperature: request.GenerationOptions?.Temperature     │  │
│  │    - HttpClient with Polly resilience (3 retries)            │  │
│  │ 4. ParseActivitiesJson(response)                             │  │
│  │    - Parses JSON array from AI response                      │  │
│  │    - Validates activity types                                │  │
│  │ 5. Returns activities or falls back to MockSessionAIService  │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              ↓                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ OpenAI API (external)                                        │  │
│  │ - Receives system + user prompts                             │  │
│  │ - Generates JSON array of activities                         │  │
│  │ - Returns completion with token usage metadata              │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

#### Detailed Request Flow

**Step 1: Client builds request** (CreateWorkshop.razor → GenerateWithAI())

```csharp
var request = new CreateSessionRequest
{
    Title = sessionTitle,
    Goal = sessionGoal,
    GenerationContext = new SessionGenerationContextDto // Optional
    {
        WorkshopType = workshopType,
        DurationMinutes = aiDurationMinutes, // e.g., 90
        ParticipantCount = aiParticipantCount,
        ParticipantTypes = new ParticipantTypesDto
        {
            Primary = "Technical" // or Business, Managers, Leaders, Mixed
        },
        ContextDocuments = new ContextDocumentsDto
        {
            SprintBacklog = new SprintBacklogDto
            {
                Provided = true,
                Summary = "Sprint 24. Payment integration...",
                KeyItems = ["JIRA-123: Stripe", "JIRA-456: Rate Limiting"]
            },
            IncidentReport = new IncidentReportDto
            {
                Provided = true,
                Summary = "Payment outage. DB connection pool exhausted.",
                Severity = "P0",
                ImpactedSystems = ["Payment API", "Database"]
            }
            // ProductDocumentation, CustomDocuments...
        }
    },
    GenerationOptions = new SessionGenerationOptionsDto
    {
        Temperature = 0.7
    }
};

var activities = await ApiService.GenerateSessionActivitiesAsync(request);
```

**Step 2: Controller receives request** (SessionsController.cs)

```csharp
[HttpPost("generate")]
public async Task<ActionResult<IReadOnlyList<AgendaActivityResponse>>> GenerateSessionActivities(
    [FromBody] CreateSessionRequest request)
{
    var activities = await _sessionAI.GenerateSessionActivitiesAsync(request);
    return Ok(activities);
}
```

**Step 3: SessionAIService builds enhanced prompt** (SessionAIService.cs)

```csharp
private string BuildUserPrompt(CreateSessionRequest request)
{
    var prompt = new StringBuilder();
    
    // Basic info
    prompt.AppendLine($"Session title: {request.Title}");
    prompt.AppendLine($"Goal: {request.Goal}");
    
    if (request.GenerationContext != null)
    {
        var ctx = request.GenerationContext;
        
        // Duration → activity count
        if (ctx.DurationMinutes > 0)
        {
            var count = CalculateActivityCount(ctx.DurationMinutes.Value);
            prompt.AppendLine($"Total duration: {ctx.DurationMinutes} minutes");
            prompt.AppendLine($"Generate {count} activities to fit this timeframe.");
        }
        
        // Participant type → terminology guidance
        if (ctx.ParticipantTypes?.Primary == "Technical")
        {
            prompt.AppendLine("Participants: Technical (Engineers, DevOps, QA)");
            prompt.AppendLine("→ Use technical terminology, focus on code quality.");
        }
        
        // Sprint backlog → reference specific items
        if (ctx.ContextDocuments?.SprintBacklog?.Provided == true)
        {
            var summary = PiiSanitizer.Sanitize(
                ctx.ContextDocuments.SprintBacklog.Summary, 500);
            prompt.AppendLine($"\n📋 Sprint Backlog:\n{summary}");
            
            foreach (var item in ctx.ContextDocuments.SprintBacklog.KeyItems)
            {
                var sanitized = PiiSanitizer.Sanitize(item, 200);
                prompt.AppendLine($"- {sanitized}");
            }
            prompt.AppendLine("→ Reference specific backlog items in questions.");
        }
        
        // Incident report → postmortem focus
        if (ctx.ContextDocuments?.IncidentReport?.Provided == true)
        {
            var summary = PiiSanitizer.Sanitize(
                ctx.ContextDocuments.IncidentReport.Summary, 500);
            prompt.AppendLine($"\n🚨 Incident Report:\n{summary}");
            prompt.AppendLine($"Severity: {ctx.ContextDocuments.IncidentReport.Severity}");
            prompt.AppendLine("→ Generate postmortem activities focusing on root cause.");
        }
    }
    
    return prompt.ToString();
}
```

**Step 4: Call OpenAI** (SessionAIService.cs)

```csharp
var payload = new
{
    model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini",
    messages = new[]
    {
        new { role = "system", content = BuildSystemPrompt() },
        new { role = "user", content = BuildUserPrompt(request) }
    },
    temperature = request.GenerationOptions?.Temperature ?? 0.7,
    max_tokens = 800
};

var client = _httpClientFactory.CreateClient("openai");
var response = await client.PostAsync(endpoint, JsonContent, cancellationToken);
var body = await response.Content.ReadAsStringAsync();
```

**Step 5: Parse response** (SessionAIService.cs)

```csharp
// Extract AI-generated JSON array
var jsonText = ExtractJsonFromResponse(body);

// Parse into AgendaActivityResponse objects
var activities = ParseActivitiesJson(jsonText);
// Returns: List of 3-7 AgendaActivityResponse with:
//   - Type (Poll, WordCloud, Rating, GeneralFeedback)
//   - Title, Prompt, DurationMinutes, Config (JSON)

return activities;
```

**Step 6: Client receives activities** (CreateWorkshop.razor)

```csharp
generatedActivities = activities.ToList();
lastGenerationInfo = $"✓ Generated {generatedActivities.Count} activities";
```

**Step 7: Facilitator reviews & finalizes** (CreateWorkshop.razor → CreateAndSaveSession())

```csharp
// Create session
var created = await ApiService.CreateSessionAsync(request);

// Get facilitator token
var token = await TokenService.GetFacilitatorTokenAsync(created.Code);

// Create each activity
foreach (var activity in generatedActivities.OrderBy(x => x.Order))
{
    var createReq = new CreateActivityRequest(
        activity.Type, activity.Order, activity.Title,
        activity.Prompt, activity.Config, activity.DurationMinutes);
    
    await ApiService.CreateActivityAsync(created.Code, createReq, token);
}

// Navigate to facilitator console
Navigation.NavigateTo($"/facilitator/console/{created.Code}");
```

#### Error Handling & Fallback

```
┌─────────────────────────────────────────────────────────────┐
│ SessionAIService.GenerateSessionActivitiesAsync()          │
├─────────────────────────────────────────────────────────────┤
│ try:                                                        │
│   1. Build prompts                                          │
│   2. Call OpenAI API                                        │
│   3. Parse JSON response                                    │
│   4. Return activities                                      │
│ catch (HttpRequestException):                               │
│   → Log error                                               │
│   → Fall back to MockSessionAIService                       │
│ catch (JsonException):                                      │
│   → Log "Failed to parse OpenAI response"                   │
│   → Fall back to MockSessionAIService                       │
└─────────────────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────────────────┐
│ MockSessionAIService                                        │
├─────────────────────────────────────────────────────────────┤
│ Returns generic activities based on workshop type:          │
│ - Retro: "What went well?", "What needs improvement?"       │
│ - Discovery: "Key challenges?", "Opportunities?"            │
│ - Incident: "What happened?", "How can we prevent this?"    │
└─────────────────────────────────────────────────────────────┘
```

#### Data Sanitization (PII Protection)

```csharp
public class PiiSanitizer
{
    public static string Sanitize(string? input, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        // Remove PII patterns
        var sanitized = input;
        sanitized = EmailRegex.Replace(sanitized, "[EMAIL]");
        sanitized = PhoneRegex.Replace(sanitized, "[PHONE]");
        sanitized = SsnRegex.Replace(sanitized, "[SSN]");
        sanitized = CreditCardRegex.Replace(sanitized, "[CARD]");
        sanitized = IpAddressRegex.Replace(sanitized, "[IP]");
        
        // Enforce max length
        return sanitized.Length > maxLength 
            ? sanitized.Substring(0, maxLength) 
            : sanitized;
    }
}
```

**Applied to**:
- Sprint backlog summary (max 500 chars)
- Sprint backlog key items (max 200 chars each)
- Incident report summary (max 500 chars)
- Product documentation summary (max 500 chars)
- All custom document summaries (max 500 chars)

**Warning logged** when PII detected:
```
[WARN] PII detected in context and sanitized for session generation
```

#### Resilience (Polly Integration)

```csharp
// Program.cs - HttpClient registration
builder.Services.AddHttpClient("openai")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry = new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        };
        options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15)
        };
        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
        options.AttemptTimeout = new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    });
```

**Behavior**:
- **Retry**: 3 attempts with exponential backoff (1s, 2s, 4s)
- **Circuit Breaker**: Opens at 50% failure rate, breaks for 15s
- **Timeouts**: 30s per attempt, 90s total

---

### Configuration

**Enable AI Session Generation**:

```json
{
  "AI": {
    "Enabled": true,
    "OpenAI": {
      "ApiKey": "sk-...",
      "Endpoint": "https://api.openai.com/v1/chat/completions",
      "Model": "gpt-4o-mini",
      "MaxTokens": 800
    }
  }
}
```

**Environment variables** (alternative):
```bash
AI__Enabled=true
AI__OpenAI__ApiKey=sk-...
AI__OpenAI__Model=gpt-4o-mini
```

**DI Registration** (Program.cs):
```csharp
var aiEnabled = builder.Configuration.GetValue<bool>("AI:Enabled");
var apiKey = builder.Configuration["AI:OpenAI:ApiKey"];

if (aiEnabled && !string.IsNullOrEmpty(apiKey))
{
    builder.Services.AddScoped<ISessionAIService, SessionAIService>();
}
else
{
    builder.Services.AddScoped<ISessionAIService, MockSessionAIService>();
}
```

---

### Security & Privacy (Session Generation Specific)

1. **PII Sanitization**:
   - All context document summaries sanitized before OpenAI
   - Removes: emails, phones, SSNs, credit cards, IP addresses
   - Enforces: 500 char max on summaries, 200 char max on list items

2. **User Warnings**:
   - UI displays: "⚠️ Do not include customer names, emails, or sensitive data"
   - Character counters prevent exceeding limits

3. **API Key Security**:
   - Never commit to source control
   - Use `appsettings.local.json` (gitignored) or environment variables
   - Recommend Azure Key Vault or AWS Secrets Manager for production

4. **Cost Control**:
   - Default model: `gpt-4o-mini` ($0.15/1M input tokens, $0.60/1M output tokens)
   - Typical cost: $0.0003-$0.0010 per generation
   - Monitor token usage via logs:
     ```
     [INF] AI session generation - Model: gpt-4o-mini, Tokens: 1250, Latency: 3200ms
     ```

5. **Rate Limiting**:
   - Recommend: Per-user or per-org quotas
   - UI prevents concurrent requests (`isGenerating` flag)
   - Circuit breaker prevents runaway API calls

---

### Implementation Notes

**Files**:
- **DTOs**: `src/TechWayFit.Pulse.Contracts/Models/SessionGenerationContextDto.cs` (8 models)
- **Request**: `src/TechWayFit.Pulse.Contracts/Requests/CreateSessionRequest.cs`
- **Service**: `src/TechWayFit.Pulse.AI/Services/SessionAIService.cs`
- **Controller**: `src/TechWayFit.Pulse.Web/Controllers/Api/SessionsController.cs`
- **UI**: `src/TechWayFit.Pulse.Web/Pages/Facilitator/CreateWorkshop.razor`

**Testing**:
- No API key → Uses `MockSessionAIService` (generic activities)
- With API key → Calls OpenAI with enhanced prompts
- Manual test checklist: See [AI-SESSION-GENERATION-IMPLEMENTATION-COMPLETE.md](./AI-SESSION-GENERATION-IMPLEMENTATION-COMPLETE.md#testing-guide)

**Monitoring**:
- Log token usage, cost, latency
- Track generation success/failure rates
- Monitor mock fallback frequency (indicates API issues)

This scenario complements the analysis/insight flow (participant AI analysis, facilitator prompts) and reuses the same AI configuration, resilience, and security patterns.

## Recommended Next Work
1. Move AI analysis to a background queue and worker to prevent request latency.
2. Define strong DTOs for AI results in `TechWayFit.Pulse.Contracts` and validate AI outputs before broadcasting.
3. Implement PII scrubbing utilities and integrate them into the prompt builder.
4. Add telemetry (Prometheus/App Insights) for AI call metrics and cost tracking.

---

Document version: 1.1
Last updated: 2026-01-31
Author: TechWayFit Engineering (automated assistant)
