# How to Add a New Activity Type

This guide covers every file that must be changed when introducing a new activity type to TechWayFit Pulse.
Steps are ordered from the data layer outward to the UI.

> **Naming convention used throughout this guide**: replace `Xxx` / `xxx` with your activity name (e.g. `Quiz`, `FiveWhys`).

---

## Overview

```
Domain  ──▶  Contracts  ──▶  API Mapper
  │                                │
  ├─ Config model                  ▼
  └─ Response payload         SessionsController (if new endpoint)
                                   │
             AI Services           │
             (exclusion / gen)     │
                                   ▼
     JS (activity-types, activity-modals, add-activities)
                                   │
             Razor components      │
             (Dashboards,          │
              Participant views,    │
              Modals, Lists)       │
                                   ▼
               MVC views (AddActivities, _ActivityFormModals)
```

---

## Step 1 — Register the Enum (2 files)

### 1a. `src/TechWayFit.Pulse.Domain/Enums/ActivityType.cs`

Add the new value at the end of the enum (assign the next integer):

```csharp
public enum ActivityType
{
    Poll = 1,
    Quiz = 2,
    WordCloud = 3,
    QnA = 4,
    Rating = 5,
    Quadrant = 6,
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

Holds the activity configuration (stored as JSON in the `Config` column).

```csharp
namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

public class XxxConfig
{
    public string SomeOption { get; set; } = string.Empty;
    public bool AnotherOption { get; set; } = true;
    // Add all configuration properties
}
```

> Skip this step only if the activity has no configuration at all.

### 2b. Response payload — `src/TechWayFit.Pulse.Domain/Models/ResponsePayloads/XxxResponse.cs`

Defines what a participant submits (stored as JSON in `Response.Payload`).

```csharp
namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

public class XxxResponse
{
    public string Answer { get; set; } = string.Empty;
    // Add all response fields
}
```

> Skip this step if the activity is read-only and participants submit nothing (e.g. AiSummary).

---

## Step 3 — Map the Enum in the API layer

### `src/TechWayFit.Pulse.Web/Api/ApiMapper.cs`

There are two switch expressions — one mapping Contracts → Domain and one mapping Domain → Contracts.
Add to **both**:

```csharp
// Contracts → Domain
ActivityType.Xxx => TechWayFit.Pulse.Domain.Enums.ActivityType.Xxx,

// Domain → Contracts
TechWayFit.Pulse.Domain.Enums.ActivityType.Xxx => ActivityType.Xxx,
```

---

## Step 4 — AI Services (3 files)

If the new activity should **not** be included when generating AI summaries, add it to the exclusion filters:

### `src/TechWayFit.Pulse.AI/Services/SessionAIService.cs`

```csharp
.Where(a => a.Type != ActivityType.AiSummary
         && a.Type != ActivityType.Break
         && a.Type != ActivityType.Xxx)   // ← add
```

### `src/TechWayFit.Pulse.AI/Services/IntelligentSessionAIService.cs`

Same filter (uses fully-qualified type name):

```csharp
&& a.Type != TechWayFit.Pulse.Contracts.Enums.ActivityType.Xxx
```

### `src/TechWayFit.Pulse.AI/Services/MLNetSessionAIService.cs`

```csharp
&& a.Type != TechWayFit.Pulse.Contracts.Enums.ActivityType.Xxx
```

If the new activity **can be auto-generated** by AI (like Poll or WordCloud), also add a case to the config-defaults switch expression in `SessionAIService.cs`:

```csharp
ActivityType.Xxx => JsonSerializer.Serialize(new { SomeOption = _activityDefaults.Xxx.SomeOption }),
```

---

## Step 5 — JavaScript: Activity Type Class

### `src/TechWayFit.Pulse.Web/wwwroot/js/activity-types.js`

Add a class that extends `Activity`. The `type` string must match the enum integer value.

```javascript
class XxxActivity extends Activity {
    constructor(data) {
        super(data);
        this.type = '10';   // matches enum value
    }

    getConfig() {
        return {
            someOption: this.someOption || '',
            anotherOption: this.anotherOption ?? true
        };
    }

    getDisplaySummary() {
        return `Xxx: ${this.title}`;
    }
}
```

---

## Step 6 — JavaScript: Save Function

### `src/TechWayFit.Pulse.Web/wwwroot/js/activity-modals.js`

Add a `window.saveXxxActivity` function that reads form values from the modal and
calls `activityManager.createActivity(...)`.

```javascript
window.saveXxxActivity = async function() {
    const title      = document.getElementById('xxxTitle')?.value?.trim();
    const someOption = document.getElementById('xxxSomeOption')?.value?.trim();
    const anotherOption = document.getElementById('xxxAnotherOption')?.checked ?? true;

    if (!title) {
        showModalError('xxxModal', 'Title is required.');
        return;
    }

    const config = JSON.stringify({ someOption, anotherOption });

    try {
        await activityManager.createActivity({
            type: 10,           // ActivityType enum value
            title,
            config,
            durationMinutes: 5  // sensible default
        });
        closeModal('xxxModal');
    } catch (err) {
        showModalError('xxxModal', 'Failed to create activity.');
        console.error(err);
    }
};
```

> **Critical**: every `getElementById` ID must exactly match the `id` attribute in the modal HTML (Steps 8 and 9).

---

## Step 7 — JavaScript: Template Config Mapping (optional)

### `src/TechWayFit.Pulse.Web/wwwroot/js/add-activities.js`

Only needed if the activity type can be AI-auto-generated. Find the `mapTemplateConfig` method and add a case:

```javascript
case 'xxx':
    return { someOption: templateConfig.someOption || '' };
```

---

## Step 8 — "Add Activity" Modal (Blazor page)

### `src/TechWayFit.Pulse.Web/Components/Facilitator/ActivityFormModals.razor`

Add the modal HTML. Use Bootstrap classes. The Save button must call `window.saveXxxActivity()`.

```html
<!-- Xxx Activity Modal -->
<div class="modal fade" id="xxxModal" tabindex="-1" aria-labelledby="xxxModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="xxxModalLabel">Add Xxx Activity</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label fw-semibold">Title</label>
                    <input type="text" class="form-control" id="xxxTitle" placeholder="Activity title" />
                </div>
                <!-- add config fields here -->
                <div class="mb-3">
                    <label class="form-label fw-semibold">Some Option</label>
                    <input type="text" class="form-control" id="xxxSomeOption" />
                </div>
                <div class="form-check mb-3">
                    <input class="form-check-input" type="checkbox" id="xxxAnotherOption" checked />
                    <label class="form-check-label" for="xxxAnotherOption">Another Option</label>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" onclick="saveXxxActivity()">Save</button>
            </div>
        </div>
    </div>
</div>
```

---

## Step 9 — "Add Activity" Modal (MVC page)

### `src/TechWayFit.Pulse.Web/Views/Shared/_ActivityFormModals.cshtml`

Copy the same modal HTML from Step 8 into this file. Same IDs, same structure.

> Both files serve the same modals: the Blazor file is used by live session pages; the MVC file is used by the add-activities page.

---

## Step 10 — "Add" Button on the Add-Activities Page

### `src/TechWayFit.Pulse.Web/Views/Facilitator/AddActivities.cshtml`

Add a button that opens the modal via Bootstrap:

```html
<button type="button"
        class="btn btn-outline-secondary"
        data-bs-toggle="modal"
        data-bs-target="#xxxModal">
    <i class="fa-solid fa-[icon-name]"></i> Xxx
</button>
```

---

## Step 11 — Participant View Component

### `src/TechWayFit.Pulse.Web/Components/Participant/Activities/XxxActivity.razor` *(new file)*

This is what participants see while the activity is active.

```razor
@using TechWayFit.Pulse.Contracts.Models
@using TechWayFit.Pulse.Domain.Models.ActivityConfigs

<div class="activity-container">
    <h4>@Activity.Title</h4>

    @if (config != null)
    {
        <!-- render the activity UI here -->
    }
</div>

@code {
    [Parameter] public ActivityResponse Activity { get; set; } = default!;

    private XxxConfig? config;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(Activity.Config))
        {
            config = System.Text.Json.JsonSerializer.Deserialize<XxxConfig>(
                Activity.Config,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
```

---

## Step 12 — Facilitator Live Dashboard Component

### `src/TechWayFit.Pulse.Web/Components/Dashboards/XxxDashboard.razor` *(new file)*

This is what the facilitator sees during a live session for this activity.
Subscribe to relevant SignalR events as needed.

```razor
@using TechWayFit.Pulse.Contracts.Models
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

<div class="card shadow-sm">
    <div class="card-body">
        <h5 class="card-title">@Activity.Title</h5>
        <!-- show live response data -->
    </div>
</div>

@code {
    [Parameter] public ActivityResponse Activity { get; set; } = default!;
    [Parameter] public HubConnection? Hub { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Hub != null)
        {
            Hub.On<object>("ResponseReceived", async (_) =>
            {
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        // unsubscribe hub handlers if needed
    }
}
```

---

## Step 13 — Wire Participant view

### `src/TechWayFit.Pulse.Web/Pages/Participant/Activity.razor`

Find the `else if` chain that dispatches to participant components and add:

```razor
else if (currentActivity.Type == ActivityType.Xxx)
{
    <XxxActivity Activity="currentActivity" />
}
```

---

## Step 14 — Wire Facilitator Live Dashboard

### `src/TechWayFit.Pulse.Web/Components/Facilitator/LiveCurrentActivity.razor`

Find the `else if` chain and add:

```razor
else if (Activity.Type == ActivityType.Xxx)
{
    <XxxDashboard Activity="Activity" Hub="Hub" />
}
```

---

## Step 15 — Activity Type Badge & Icon

### `src/TechWayFit.Pulse.Web/Components/Facilitator/LiveActivitiesList.razor`

Find the `GetActivityTypeIcon` and `GetActivityTypeBadgeStyle` switch/if blocks and add cases:

```csharp
// icon
ActivityType.Xxx => "<i class=\"fa-solid fa-[icon-name]\"></i>",

// badge colour
ActivityType.Xxx => "background-color: #rrggbb; color: #fff;",
```

---

## Step 16 — Edit Activity Modal (live session)

### `src/TechWayFit.Pulse.Web/Components/Facilitator/EditActivityModal.razor`

Find the `else if` chain that renders config fields and add:

```razor
else if (Activity.Type == ActivityType.Xxx)
{
    <div class="mb-3">
        <label class="form-label">Some Option</label>
        <input class="form-control" @bind="editSomeOption" />
    </div>
}
```

Also handle loading and saving in the codebehind (`EditActivityModal.razor.cs`) as appropriate.

---

## Step 17 — Presentation Mode

### `src/TechWayFit.Pulse.Web/Pages/Facilitator/Presentation.razor`

Find the `else if` chain that builds the presentation slide view and add:

```razor
else if (activity.Type == ActivityType.Xxx)
{
    <!-- presentation-mode HTML for Xxx -->
    <div class="slide-content">
        <h2>@activity.Title</h2>
    </div>
}
```

### `src/TechWayFit.Pulse.Web/Pages/Facilitator/Presentation.razor.cs`

Add any helper methods for deserialising config or building display data:

```csharp
private XxxConfig? GetXxxConfig(ActivityResponse activity)
{
    if (string.IsNullOrEmpty(activity.Config)) return null;
    return System.Text.Json.JsonSerializer.Deserialize<XxxConfig>(
        activity.Config,
        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
```

---

## Optional — New API Endpoint

Only needed if the activity requires a server-side action beyond the standard CRUD
(e.g. AiSummary needs a `POST .../generate-summary` endpoint).

### `src/TechWayFit.Pulse.Web/Controllers/Api/SessionsController.cs`

```csharp
[HttpPost("{code}/activities/{activityId}/xxx-action")]
[RequireFacilitatorToken]
public async Task<IActionResult> XxxAction(string code, Guid activityId)
{
    // ...
}
```

---

## Optional — Custom JavaScript Utilities

If the new activity needs browser-side behaviour (timers, drag-drop, etc.) that can't
live in `activity-types.js`, create a dedicated file:

### `src/TechWayFit.Pulse.Web/wwwroot/js/xxx-helper.js` *(new file)*

Then register it in the layout:

### `src/TechWayFit.Pulse.Web/Views/Shared/_Layout.cshtml`

```html
<script src="~/js/xxx-helper.js"></script>
```

---

## Checklist

| # | File | Change |
|---|------|--------|
| 1 | `Domain/Enums/ActivityType.cs` | Add enum value |
| 2 | `Contracts/Enums/ActivityType.cs` | Add matching enum value |
| 3 | `Domain/Models/ActivityConfigs/XxxConfig.cs` | **New file** — config model |
| 4 | `Domain/Models/ResponsePayloads/XxxResponse.cs` | **New file** — response payload (if applicable) |
| 5 | `Web/Api/ApiMapper.cs` | Map enum both ways |
| 6 | `AI/Services/SessionAIService.cs` | Exclude from AI summary (if applicable) |
| 7 | `AI/Services/IntelligentSessionAIService.cs` | Same exclusion |
| 8 | `AI/Services/MLNetSessionAIService.cs` | Same exclusion |
| 9 | `wwwroot/js/activity-types.js` | Add `XxxActivity` class |
| 10 | `wwwroot/js/activity-modals.js` | Add `saveXxxActivity()` function |
| 11 | `wwwroot/js/add-activities.js` | Add template config mapping (if AI-generated) |
| 12 | `Components/Facilitator/ActivityFormModals.razor` | Add modal HTML |
| 13 | `Views/Shared/_ActivityFormModals.cshtml` | Add same modal HTML |
| 14 | `Views/Facilitator/AddActivities.cshtml` | Add "Add Xxx" button |
| 15 | `Components/Participant/Activities/XxxActivity.razor` | **New file** — participant view |
| 16 | `Components/Dashboards/XxxDashboard.razor` | **New file** — facilitator dashboard |
| 17 | `Pages/Participant/Activity.razor` | Wire participant component |
| 18 | `Components/Facilitator/LiveCurrentActivity.razor` | Wire facilitator dashboard |
| 19 | `Components/Facilitator/LiveActivitiesList.razor` | Add icon and badge style |
| 20 | `Components/Facilitator/EditActivityModal.razor` | Add edit config fields |
| 21 | `Pages/Facilitator/Presentation.razor` | Add presentation view |
| 22 | `Pages/Facilitator/Presentation.razor.cs` | Add config helper method |
| 23 | `Controllers/Api/SessionsController.cs` | New endpoint *(optional)* |
| 24 | `wwwroot/js/xxx-helper.js` + `_Layout.cshtml` | New JS utility *(optional)* |

---

## Common Mistakes

- **ID mismatch between JS and HTML**: Every `getElementById('someId')` in `activity-modals.js` must exactly match an `id="someId"` in the modal HTML. A mismatch silently returns `null` and the save function exits without error.
- **Forgetting both modal files**: The Blazor `.razor` file powers the live session pages; the MVC `.cshtml` file powers the add-activities page. Both need the modal.
- **Skipping the ApiMapper**: The Contracts and Domain enums are separate. Without a mapping, activity type is silently set to the default (`Poll = 1`).
- **Not excluding from AI summary**: If a non-participatory activity (like a Break) is included in AI summary generation, it will confuse the model with empty response data.
