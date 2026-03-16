# How to Add a New Activity Type

This guide reflects the **activity plugin architecture** introduced in March 2026.
Adding a new activity type now requires **5 steps** touching at most **7 files**, compared to the previous 24-step, 22-file process.

> **Naming convention used throughout**: replace `Xxx` / `xxx` with your activity name (e.g. `Quiz`, `Brainstorm`).

---

## How it Works

Every activity type is a **self-contained plugin** made of two parts:

| Part | Layer | Responsibility |
|---|---|---|
| `IActivityPlugin` | Application | Business logic: config defaults, validation, dashboard data |
| `IActivityUiDescriptor` | Web | UI wiring: which Blazor components to render |

Both registries are populated via DI (`IEnumerable<T>`) — the dispatch chains in all Razor pages are gone and **do not need to be touched**.

---

## Step 1 — Register the Enum (2 files)

### 1a. `src/TechWayFit.Pulse.Domain/Enums/ActivityType.cs`

Add the new value at the end:

```csharp
public enum ActivityType
{
    Poll = 0,
    Quiz = 1,
    WordCloud = 2,
    QnA = 3,
    Rating = 4,
    Quadrant = 5,
    FiveWhys = 6,
    GeneralFeedback = 7,
    AiSummary = 8,
    Break = 9,
    Xxx = 10   // ← add here
}
```

### 1b. `src/TechWayFit.Pulse.Contracts/Enums/ActivityType.cs`

Mirror the same value in the Contracts enum (used by the API layer):

```csharp
Xxx = 10
```

---

## Step 2 — Create Data Models

### 2a. Config model — `src/TechWayFit.Pulse.Domain/Models/ActivityConfigs/XxxConfig.cs`

```csharp
namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

public class XxxConfig
{
    public string SomeSetting { get; set; } = string.Empty;
    // Add all configuration properties
}
```

### 2b. Response payload — `src/TechWayFit.Pulse.Domain/Models/ResponsePayloads/XxxResponse.cs`

```csharp
namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

public class XxxResponse
{
    public string Answer { get; set; } = string.Empty;
    // Add all response fields
}
```

> Skip 2b if the activity is read-only (participants submit nothing).

---

## Step 3 — Create the Application Plugin (2 files)

### 3a. Dashboard data — `src/TechWayFit.Pulse.Application/Activities/Plugins/Xxx/XxxDashboardData.cs`

Wraps the dashboard data and implements `IActivityDashboardData`:

```csharp
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Xxx;

public sealed class XxxDashboardData : IActivityDashboardData
{
    public XxxDashboardData(
        Guid sessionId, Guid activityId, string activityTitle,
        int totalResponses, int participantCount, int respondedParticipants,
        DateTimeOffset? lastResponseAt)
    {
        SessionId            = sessionId;
        ActivityId           = activityId;
        ActivityTitle        = activityTitle;
        TotalResponses       = totalResponses;
        ParticipantCount     = participantCount;
        RespondedParticipants = respondedParticipants;
        LastResponseAt       = lastResponseAt;
    }

    public Guid SessionId            { get; }
    public Guid ActivityId           { get; }
    public string ActivityTitle      { get; }
    public int TotalResponses        { get; }
    public int ParticipantCount      { get; }
    public int RespondedParticipants { get; }
    public DateTimeOffset? LastResponseAt { get; }

    // Add any activity-specific computed properties the dashboard component needs:
    // public IReadOnlyList<XxxResultItem> Results { get; }
}
```

> If a typed `XxxDashboardResponse` record exists in `Contracts/Responses/`, use that as a wrapper (see `PollDashboardData` for the pattern) instead of storing fields directly.

### 3b. Plugin — `src/TechWayFit.Pulse.Application/Activities/Plugins/Xxx/XxxActivityPlugin.cs`

```csharp
using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Xxx;

public sealed class XxxActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.Xxx;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName:        "Xxx",
        FaIconClass:        "fa-solid fa-[icon-name]",
        BadgeColorHex:      "#rrggbb",
        BadgeTextColorHex:  "#ffffff",
        AcceptsResponses:   true,
        ConfigType:         typeof(XxxConfig),
        ResponsePayloadType: typeof(XxxResponse));  // null if read-only

    // ── Config ────────────────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"someSetting":"default"}""";

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        // Apply any per-plugin server limits. Return config unchanged if none apply.
        if (string.IsNullOrWhiteSpace(config)) return GetDefaultConfig();
        return config;
    }

    public bool ValidateConfig(string? config, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(config))
            list.Add("Xxx config is required.");
        errors = list;
        return list.Count == 0;
    }

    // ── Response ──────────────────────────────────────────────────────────────

    public bool AcceptsResponses => Metadata.AcceptsResponses;

    public bool ValidateResponsePayload(string payload, out string? error)
    {
        if (string.IsNullOrWhiteSpace(payload)) { error = "Payload is required."; return false; }
        error = null;
        return true;
    }

    // ── AI participation ──────────────────────────────────────────────────────

    public bool IncludeInAiSummary => true;   // false for Break / AiSummary
    public bool CanBeAiGenerated   => true;   // false for AiSummary

    // ── Dashboard data ────────────────────────────────────────────────────────

    public async Task<IActivityDashboardData> GetDashboardDataAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        IActivityDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        var activity     = await dataContext.GetActivityAsync(activityId, cancellationToken)
                           ?? throw new ArgumentException("Activity not found.", nameof(activityId));
        var responses    = await dataContext.GetResponsesAsync(activityId, cancellationToken);
        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);

        // TODO: compute activity-specific results from responses

        return new XxxDashboardData(
            sessionId,
            activityId,
            activity.Title,
            totalResponses:       responses.Count,
            participantCount:     participants.Count,
            respondedParticipants: responses.Select(r => r.ParticipantId).Distinct().Count(),
            lastResponseAt:       responses.Count == 0 ? null : responses.Max(r => r.CreatedAt));
    }
}
```

### 3c. DI extension — `src/TechWayFit.Pulse.Application/Activities/Plugins/Xxx/XxxActivityServiceExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Xxx;

public static class XxxActivityServiceExtensions
{
    public static IServiceCollection AddXxxActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, XxxActivityPlugin>();
        return services;
    }
}
```

---

## Step 4 — Create the Blazor Components

> These components are the same as before — no architectural change here.

### 4a. Participant view — `src/TechWayFit.Pulse.Web/Components/Participant/Activities/XxxActivity.razor`

What a participant sees while the activity is active.

```razor
@using TechWayFit.Pulse.Web.Components.Participant.Activities

<div class="xxx-activity">
    <!-- participant UI -->
</div>

@code {
    [Parameter] public ParticipantActivityParameters Parameters { get; set; } = default!;
}
```

### 4b. Facilitator dashboard — `src/TechWayFit.Pulse.Web/Components/Dashboards/XxxDashboard.razor`

Live facilitator view. Must accept `SessionCode` (string) and `ActivityId` (Guid) parameters.

```razor
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

<div class="card shadow-sm">
    <div class="card-body">
        <!-- live dashboard UI -->
    </div>
</div>

@code {
    [Parameter, EditorRequired] public string SessionCode { get; set; } = default!;
    [Parameter, EditorRequired] public Guid ActivityId { get; set; }

    public async ValueTask DisposeAsync() { /* unsubscribe hub */ }
}
```

### 4c. Presentation view (optional) — `src/TechWayFit.Pulse.Web/Components/Presentation/XxxPresentation.razor`

Full-screen facilitator presentation mode. Must accept at minimum: `SessionCode`, `ActivityId`, `ActivityTitle`, `ActivityPrompt`, `DurationMinutes`, `OpenedAt`.

```razor
@code {
    [Parameter, EditorRequired] public required string SessionCode { get; set; }
    [Parameter, EditorRequired] public required Guid ActivityId { get; set; }
    [Parameter] public string? ActivityTitle { get; set; }
    [Parameter] public string? ActivityPrompt { get; set; }
    [Parameter] public int? DurationMinutes { get; set; }
    [Parameter] public DateTimeOffset? OpenedAt { get; set; }
}
```

> If your presentation component needs **extra** parameters (e.g. `ActivityConfig`), override `BuildPresentationParameters()` on the UI descriptor (see Step 5).

---

## Step 5 — Create the Web UI Descriptor and Wire It Up (2 files)

### 5a. UI descriptor — `src/TechWayFit.Pulse.Web/Activities/Plugins/Xxx/XxxActivityUiDescriptor.cs`

Maps the activity type to its Blazor components. `IActivityUiRegistry` discovers it automatically.

```csharp
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Components.Dashboards;
using TechWayFit.Pulse.Web.Components.Participant.Activities;
using TechWayFit.Pulse.Web.Components.Presentation;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Xxx;

public sealed class XxxActivityUiDescriptor : IActivityUiDescriptor
{
    public ActivityType ActivityType => ActivityType.Xxx;

    public Type? ParticipantComponentType   => typeof(XxxActivity);
    public Type? DashboardComponentType     => typeof(XxxDashboard);
    public Type? PresentationComponentType  => typeof(XxxPresentation);  // null if no presentation
    public Type? EditConfigComponentType    => null;
    public Type? CreateModalComponentType   => null;

    // Override only if your components need EXTRA parameters beyond the standard set.
    // public IDictionary<string, object?> BuildDashboardParameters(...) => ...
    // public IDictionary<string, object?> BuildPresentationParameters(...) => ...
}
```

### 5b. DI extension — `src/TechWayFit.Pulse.Web/Activities/Plugins/Xxx/XxxWebServiceExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Xxx;

public static class XxxWebServiceExtensions
{
    public static IServiceCollection AddXxxActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, XxxActivityUiDescriptor>();
        return services;
    }
}
```

### 5c. Register in `AllActivityPluginsExtensions.cs`

`src/TechWayFit.Pulse.Web/Activities/AllActivityPluginsExtensions.cs` — the single place all plugins are wired in:

```csharp
// Application-layer plugin
services.AddXxxActivity();

// Web-layer UI descriptor
services.AddXxxActivityUi();
```

Both registrations belong in `AddAllActivityPlugins()` alongside the other activities.

---

## Additional Tasks (context-dependent)

### A — API Mapper

If your new enum value must cross the Contracts ↔ Domain boundary through the existing mapper, add it to **both** switch expressions in `src/TechWayFit.Pulse.Web/Api/ApiMapper.cs`:

```csharp
// Contracts → Domain
ActivityType.Xxx => TechWayFit.Pulse.Domain.Enums.ActivityType.Xxx,

// Domain → Contracts
TechWayFit.Pulse.Domain.Enums.ActivityType.Xxx => ActivityType.Xxx,
```

### B — AI config-defaults (if AI-generated)

If the activity can be auto-generated by AI (`CanBeAiGenerated = true`), add a config-build case to the switch expression in `src/TechWayFit.Pulse.AI/Services/SessionAIService.cs`:

```csharp
ActivityType.Xxx => JsonSerializer.Serialize(new { SomeSetting = "default" }),
```

The AI summary exclusion (`IncludeInAiSummary`, `CanBeAiGenerated`) is declared on the plugin itself and read by `IActivityRegistry` — **no changes needed** to the three AI service files for inclusion/exclusion.

### C — "Add Activity" modal

Create the modal HTML and the `saveXxxActivity()` JS function following the same patterns as the existing modals in:

- `src/TechWayFit.Pulse.Web/Components/Facilitator/ActivityFormModals.razor`
- `src/TechWayFit.Pulse.Web/Views/Shared/_ActivityFormModals.cshtml`
- `src/TechWayFit.Pulse.Web/wwwroot/js/activity-modals.js`
- `src/TechWayFit.Pulse.Web/Views/Facilitator/AddActivities.cshtml` (Add button)
- `src/TechWayFit.Pulse.Web/wwwroot/js/activity-types.js` (Activity class)

### D — Edit-activity config fields

If the facilitator should be able to edit config inline during a live session, add the edit fields in `src/TechWayFit.Pulse.Web/Components/Facilitator/EditActivityModal.razor` (and its codebehind).

### E — Custom API endpoint

Only needed for activities with non-standard server-side actions (e.g. AiSummary's `generate-summary`). Add to `src/TechWayFit.Pulse.Web/Controllers/Api/SessionsController.cs`.

---

## Checklist

| # | File | Change |
|---|------|--------|
| 1 | `Domain/Enums/ActivityType.cs` | Add enum value |
| 2 | `Contracts/Enums/ActivityType.cs` | Add matching enum value |
| 3 | `Domain/Models/ActivityConfigs/XxxConfig.cs` | **New file** — config model |
| 4 | `Domain/Models/ResponsePayloads/XxxResponse.cs` | **New file** — response payload (if applicable) |
| 5 | `Application/Activities/Plugins/Xxx/XxxDashboardData.cs` | **New file** — dashboard data |
| 6 | `Application/Activities/Plugins/Xxx/XxxActivityPlugin.cs` | **New file** — plugin |
| 7 | `Application/Activities/Plugins/Xxx/XxxActivityServiceExtensions.cs` | **New file** — DI |
| 8 | `Web/Components/Participant/Activities/XxxActivity.razor` | **New file** — participant view |
| 9 | `Web/Components/Dashboards/XxxDashboard.razor` | **New file** — facilitator dashboard |
| 10 | `Web/Components/Presentation/XxxPresentation.razor` | **New file** — presentation *(optional)* |
| 11 | `Web/Activities/Plugins/Xxx/XxxActivityUiDescriptor.cs` | **New file** — UI wiring |
| 12 | `Web/Activities/Plugins/Xxx/XxxWebServiceExtensions.cs` | **New file** — DI |
| 13 | `Web/Activities/AllActivityPluginsExtensions.cs` | Add two registration calls |
| 14 | `Web/Api/ApiMapper.cs` | Map enum both ways *(if needed)* |
| 15 | `AI/Services/SessionAIService.cs` | Add config-build case *(if AI-generated)* |
| 16 | `Web/Components/Facilitator/ActivityFormModals.razor` + JS + MVC | "Add" modal *(if applicable)* |
| 17 | `Web/Components/Facilitator/EditActivityModal.razor` | Edit config fields *(if applicable)* |
| 18 | `Controllers/Api/SessionsController.cs` | Custom endpoint *(if applicable)* |

**Required for every new type: steps 1–13.** Steps 14–18 are context-dependent.

---

## What You Do NOT Need to Touch

The following files required changes under the old architecture but are **fully automatic** now:

| File (old requirement) | Why it is no longer needed |
|---|---|
| `Pages/Participant/Activity.razor` — `if/else` chain | `DynamicComponent` reads from `IActivityUiRegistry` |
| `Components/Facilitator/LiveCurrentActivity.razor` — `if/else` chain | Same |
| `Pages/Facilitator/Presentation.razor` — `if/else` chain | Same |
| `AI/Services/SessionAIService.cs` — exclusion filter | Driven by `IncludeInAiSummary` on the plugin |
| `AI/Services/IntelligentSessionAIService.cs` — exclusion filter | Same |
| `AI/Services/MLNetSessionAIService.cs` — exclusion filter | Same |

---

## Common Mistakes

- **Forgetting `AllActivityPluginsExtensions.cs`**: Both `AddXxxActivity()` and `AddXxxActivityUi()` must be added. Missing one silently falls through to a "coming soon" fallback.
- **Non-standard component parameters**: If your dashboard or presentation component has extra parameters beyond the standard set (e.g. `ActivityConfig`), override `BuildDashboardParameters()` or `BuildPresentationParameters()` in your `IActivityUiDescriptor`. See `QuadrantActivityUiDescriptor` for a reference.
- **Skipping the ApiMapper**: The Contracts and Domain enums each have their own definition. Without a mapping entry in `ApiMapper.cs`, the activity type silently defaults to `Poll`.
- **Enum value conflicts**: Check both enum files before picking an integer. Values must be unique and must match between Domain and Contracts.

