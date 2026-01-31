# AI Session Create Scenario

This document describes the end-to-end flow for generating a workshop session agenda with AI, reviewing/editing the generated activities in the facilitator UI, and persisting the session and activities to the server.

Overview
- The facilitator uses the `Create Workshop` UI to provide a title, goal and optional context and clicks `Generate with AI`.
- The client posts a session-generation request to the server (client-side helper: `PulseApiService.GenerateSessionActivitiesAsync`, server controller: `SessionsController`), which invokes `ISessionAIService.GenerateSessionActivitiesAsync`.
- `ISessionAIService` will call OpenAI when `AI:OpenAI:ApiKey` is configured (and `AI:Enabled` is true). If the API key is missing or an error occurs, the service falls back to the mock generator.

Typical step-by-step flow
1. Facilitator submits the generation request from the `Create Workshop` page.
2. Server returns a list of `AgendaActivityResponse` items (3–7 activities recommended).
3. The facilitator reviews the generated activities in the UI. Edits and deletes performed here are in "draft" mode (client-side only) and are not persisted until the session is created.
4. When ready, the facilitator clicks `Save & Go Live`:
   - Client calls `POST /api/sessions` (create session) with the session metadata.
   - Server responds with the created session and session `Code`.
   - Client obtains a facilitator token (the codebase uses `IClientTokenService` which may call the facilitator join API as a fallback).
   - Client iterates the locally-drafted activities and creates each activity by calling `POST /api/sessions/{code}/activities` (using the facilitator token).
   - Finally the client navigates to the facilitator console for the new session.

Implementation notes and guidance
- Endpoint mapping: generation is handled by the `SessionsController` (generate action), activity creation uses the sessions activities API.
- Draft edits: the facilitator review UI (component: `EditActivityModal`) supports draft callbacks so that changes are applied locally without persisting until the facilitator finalizes the session.
- AI service wiring: `Program.cs` registers a named HttpClient `openai` and conditionally registers real or mock AI services depending on `AI:Enabled` and `AI:OpenAI:ApiKey`.
- Configuration to enable real OpenAI generation:
  - Add `AI:Enabled: true` and `AI:OpenAI:ApiKey` (and optional `AI:OpenAI:Endpoint`/`Model`) to `appsettings.local.json`, environment variables, or user secrets.
- Failure modes and fallback:
  - If OpenAI calls fail or the response cannot be parsed to valid JSON, `SessionAIService` falls back to the `MockSessionAIService` and the UI receives the mock activities.
  - The UI should show friendly error messages when generation fails and allow retry; sensitive content warnings should be presented before sending facilitator-provided context to the AI.

Security & privacy reminders specific to session generation
- Always scrub PII from context before sending to the model. The design recommends limiting context size (e.g., summarised to ~500 characters) and removing emails, phone numbers, and other identifiers.
- Do not commit API keys; use secret storage. Monitor token usage and enforce rate limits or per-org quotas to control costs.

Notes
- This companion doc complements `docs/ai/ai-integration.md`. I can merge this content into the main `ai-integration.md` file if you prefer — the repo's `ai-integration.md` is currently a fenced markdown block so I avoided editing it directly to prevent accidental formatting issues.
