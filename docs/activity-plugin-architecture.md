# Activity Plugin Architecture — Architectural Analysis & Design

**Date:** 16 March 2026  
**Author:** Solution Architecture  
**Status:** Proposal

---

## 1. The Current Problem — Shotgun Surgery

Each activity type in TechWayFit Pulse is **not a cohesive unit**. Its behaviour, configuration, display logic, and AI participation rules are spread across 22+ files in 6+ projects. The project's own [how-to-add-new-activity-type.md](how-to-add-new-activity-type.md) documents exactly 24 touch-points.

This is the textbook definition of **Shotgun Surgery** (from Martin Fowler's *Refactoring*): a single conceptual change forces you to make many small changes in many different places.

### The 9 Dispatch Chains That Own You

Every `if/else` chain below must be updated **every time a new activity is added or an existing one changes**:

| File | Chain Purpose | Current Size |
|---|---|---|
| `LiveCurrentActivity.razor` | Facilitator dashboard dispatch | 9 branches |
| `Pages/Participant/Activity.razor` | Participant UI dispatch | 9 branches |
| `Pages/Facilitator/Presentation.razor` | Presentation mode dispatch | 8 branches |
| `EditActivityModal.razor` | Edit config fields dispatch | 7 branches |
| `SessionAIService.cs` – `GenerateDefaultConfig` | AI default config generation | 7 branches |
| `SessionAIService.cs` – `EnforceActivityLimits` | Config limit enforcement | 4 branches |
| `IntelligentSessionAIService.cs` – filter | AI exclusion filter | manual list |
| `MLNetSessionAIService.cs` – filter | AI exclusion filter | manual list |
| `ApiMapper.cs` | Enum conversion (both directions) | 2 × 9 branches |

### The 7 Service Interface Proliferation

Every interactive activity type has its own dedicated Application-layer interface and implementation that are largely structurally identical:

```
IActivityService / IDashboardService / IPollDashboardService / IWordCloudDashboardService /
IRatingDashboardService / IGeneralFeedbackDashboardService / IQnADashboardService /
IQuadrantDashboardService / IFiveWhysAIService / ...
```

Each is also individually registered in `Program.cs`. Adding a `Voting` activity means adding one more interface, one more class, one more registration, and one more branch in `SessionsController`.

### What This Costs in Practice

| Metric | Current |
|---|---|
| Files changed to add one activity type | **22 files minimum** |
| Projects touched | **6 projects** |
| Minimum lines of understanding before a contributor can safely add an activity | **>2,000** |
| Risk of breaking an unrelated activity when editing a dispatch chain | **High** |
| Can activities be extracted into separate deployable plugins? | **No** |
| Can an activity be removed without leaving dead code? | **No** |

---

## 2. Root Cause — No Activity Abstraction Boundary

The fundamental design gap is that **there is no single interface that represents "an activity type"**. The system has `ActivityType` (an enum) and dozens of concrete classes, but there is nothing that says:

> "For any `ActivityType`, here is everything you need to know about it."

Without that contract, every piece of code that needs to know something about `ActivityType.Poll` specifically goes and asks by casting or branching on the enum value. The enum becomes a polymorphism escape hatch, and the codebase leaks activity-specific knowledge everywhere.

### The Open/Closed Principle Failure

The code is **not closed for modification** when adding activity types. It should be: you should be able to add `ActivityType.Voting` without touching any existing file — only registering a new module.

---

## 3. The Proposed Architecture — Activity Plugin Registry

### 3.1 Mental Model

Each activity type goes from being an enum value scattered across the codebase, to a **self-contained plugin** that owns all its knowledge in one place.

```
Before                                  After
──────────────────────────────────────  ──────────────────────────────────────
ActivityType.Poll (enum value)           PollActivityPlugin implements IActivityPlugin
  + PollDashboardService.cs             │  ├─ Type = ActivityType.Poll
  + PollConfig.cs                       │  ├─ Metadata (icon, color, label)
  + PollResponse.cs                     │  ├─ GetDefaultConfig()
  + IPollDashboardService.cs            │  ├─ EnforceConfigLimits()
  + if/else in Presentation.razor       │  ├─ GetDashboardDataAsync()
  + if/else in Activity.razor           │  ├─ ParticipantComponentType
  + if/else in LiveCurrentActivity.razor│  ├─ DashboardComponentType
  + if/else in AI services (×3)         │  ├─ IncludeInAiSummary
  + ApiMapper switch (×2)               │  └─ CanBeAiGenerated
  + EditActivityModal.razor             
  + ActivityFormModals.razor            
  + _ActivityFormModals.cshtml          
  + activity-types.js                   
  + activity-modals.js                  
  + add-activities.js (maybe)           
```

### 3.2 Core Interfaces

These live in a new `TechWayFit.Pulse.Activities.Abstractions` project (or `Application/Activities/` namespace initially):

```csharp
// ────────────────────────────────────────────────────────────────────────────
// The central contract every activity type must implement.
// This is the ONLY place you need to add code when creating a new activity.
// ────────────────────────────────────────────────────────────────────────────
public interface IActivityPlugin
{
    // Identity
    ActivityType ActivityType { get; }

    // Display
    ActivityPluginMetadata Metadata { get; }

    // Config contract
    string GetDefaultConfig();
    string EnforceConfigLimits(string? config, IActivityDefaults defaults);
    bool ValidateConfig(string? config, out IReadOnlyList<string> errors);

    // Response contract
    bool AcceptsResponses { get; }  // false for Break, AiSummary
    bool ValidateResponsePayload(string payload, out string? error);

    // AI participation
    bool IncludeInAiSummary { get; }   // false for Break, AiSummary
    bool CanBeAiGenerated { get; }

    // Dashboard (returns type-safe data — callers cast using Metadata.ResponseType)
    Task<IActivityDashboardData> GetDashboardDataAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        IActivityDataContext dataContext,
        CancellationToken cancellationToken = default);

    // Blazor component types (eliminates all if/else chains in .razor files)
    Type? ParticipantComponentType   { get; }   // e.g. typeof(PollActivity)
    Type? DashboardComponentType     { get; }   // e.g. typeof(PollDashboard)
    Type? PresentationComponentType  { get; }   // e.g. typeof(PollPresentation)
    Type? EditConfigComponentType    { get; }   // e.g. typeof(PollEditConfig)
    Type? CreateModalComponentType   { get; }   // e.g. typeof(PollCreateModal)
}

// Marker interface for type-safe dashboard data passing
public interface IActivityDashboardData { }

// Data access context injected into plugins (hides IFooRepository from plugins)
public interface IActivityDataContext
{
    Task<IReadOnlyList<Response>> GetResponsesAsync(Guid activityId, CancellationToken ct);
    Task<IReadOnlyList<Participant>> GetParticipantsAsync(Guid sessionId, CancellationToken ct);
    Task<Activity?> GetActivityAsync(Guid activityId, CancellationToken ct);
}

// Immutable metadata record
public sealed record ActivityPluginMetadata(
    string DisplayName,          // "Poll"
    string FaIconClass,          // "fa-solid fa-chart-bar"
    string BadgeColorHex,        // "#0d6efd"
    string BadgeTextColorHex,    // "#fff"
    bool   RequiresResponseSubmission,
    Type   ConfigType,           // typeof(PollConfig)
    Type?  ResponsePayloadType); // typeof(PollResponse)
```

### 3.3 The Activity Registry

```csharp
// Single lookup point — replaces all if/else dispatch chains
public interface IActivityRegistry
{
    IActivityPlugin GetPlugin(ActivityType type);
    IReadOnlyList<IActivityPlugin> GetAll();
    IReadOnlyList<IActivityPlugin> GetAiGeneratable();
    IReadOnlyList<IActivityPlugin> GetIncludedInAiSummary();
}

public sealed class ActivityRegistry : IActivityRegistry
{
    private readonly IReadOnlyDictionary<ActivityType, IActivityPlugin> _plugins;

    public ActivityRegistry(IEnumerable<IActivityPlugin> plugins)
    {
        _plugins = plugins.ToDictionary(p => p.ActivityType);
    }

    public IActivityPlugin GetPlugin(ActivityType type)
        => _plugins.TryGetValue(type, out var p) ? p
           : throw new InvalidOperationException($"No plugin registered for activity type '{type}'.");

    public IReadOnlyList<IActivityPlugin> GetAll()
        => _plugins.Values.ToList();

    public IReadOnlyList<IActivityPlugin> GetAiGeneratable()
        => _plugins.Values.Where(p => p.CanBeAiGenerated).ToList();

    public IReadOnlyList<IActivityPlugin> GetIncludedInAiSummary()
        => _plugins.Values.Where(p => p.IncludeInAiSummary).ToList();
}
```

### 3.4 How Each Activity Looks After Refactoring

**Example: Poll activity becomes fully self-contained:**

```csharp
// TechWayFit.Pulse.Activities.Poll / PollActivityPlugin.cs
public sealed class PollActivityPlugin : IActivityPlugin
{
    public ActivityType ActivityType => ActivityType.Poll;

    public ActivityPluginMetadata Metadata => new(
        DisplayName: "Poll",
        FaIconClass: "fa-solid fa-chart-bar",
        BadgeColorHex: "#0d6efd",
        BadgeTextColorHex: "#fff",
        RequiresResponseSubmission: true,
        ConfigType: typeof(PollConfig),
        ResponsePayloadType: typeof(PollResponse));

    public bool AcceptsResponses => true;
    public bool IncludeInAiSummary => true;
    public bool CanBeAiGenerated => true;

    public string GetDefaultConfig()
        => JsonSerializer.Serialize(new PollConfig { MaxResponsesPerParticipant = 1 });

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        var cfg = JsonSerializer.Deserialize<PollConfig>(config ?? "{}") ?? new();
        cfg.MaxResponsesPerParticipant = Math.Min(
            cfg.MaxResponsesPerParticipant, defaults.Poll.MaxResponsesPerParticipant);
        return JsonSerializer.Serialize(cfg);
    }

    public bool ValidateConfig(string? config, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(config)) { errors = list; return true; }
        var cfg = JsonSerializer.Deserialize<PollConfig>(config);
        if (cfg?.Options?.Count < 2) list.Add("A poll requires at least 2 options.");
        errors = list;
        return list.Count == 0;
    }

    public bool ValidateResponsePayload(string payload, out string? error)
    {
        var resp = JsonSerializer.Deserialize<PollResponse>(payload);
        error = resp?.SelectedOptionId == null ? "No option selected." : null;
        return error is null;
    }

    public async Task<IActivityDashboardData> GetDashboardDataAsync(
        Guid sessionId, Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        IActivityDataContext ctx, CancellationToken ct)
    {
        // All the logic currently in PollDashboardService lives here
        var responses = await ctx.GetResponsesAsync(activityId, ct);
        // ... compute and return PollDashboardData (implements IActivityDashboardData)
    }

    // These replace all if/else chains in .razor files
    public Type? ParticipantComponentType   => typeof(PollActivity);
    public Type? DashboardComponentType     => typeof(PollDashboard);
    public Type? PresentationComponentType  => typeof(PollPresentation);
    public Type? EditConfigComponentType    => typeof(PollEditConfig);
    public Type? CreateModalComponentType   => typeof(PollCreateModal);
}
```

### 3.5 Eliminating the Razor `if/else` Chains

Blazor's built-in `DynamicComponent` replaces all dispatch chains in `.razor` files:

**Before (LiveCurrentActivity.razor — 50+ lines, grows with every activity):**
```razor
@if (Activity.Type == ActivityType.Poll)          { <PollDashboard ... /> }
else if (Activity.Type == ActivityType.WordCloud)  { <WordCloudDashboard ... /> }
else if (Activity.Type == ActivityType.Rating)     { <RatingDashboard ... /> }
else if (Activity.Type == ActivityType.QnA)        { <QnADashboard ... /> }
@* ... 5 more branches, each new activity adds one *@
```

**After (LiveCurrentActivity.razor — stable, never changes):**
```razor
@inject IActivityRegistry ActivityRegistry

@{
    var plugin = ActivityRegistry.GetPlugin(Activity.Type);
    var componentType = plugin.DashboardComponentType;
    var parameters = new Dictionary<string, object> {
        [nameof(SessionCode)] = SessionCode,
        [nameof(ActivityId)] = Activity.ActivityId
    };
}

@if (componentType != null)
{
    <DynamicComponent Type="@componentType" Parameters="@parameters" />
}
else
{
    <div class="empty-state">Dashboard not available for @plugin.Metadata.DisplayName</div>
}
```

The same pattern applies to `Activity.razor` (participant view), `Presentation.razor`, and `EditActivityModal.razor`. **All four dispatch chains disappear permanently.**

### 3.6 Eliminating the AI Service Hardcoded Lists

**Before (SessionAIService.cs — must be edited for every new non-participatory activity):**
```csharp
.Where(a => a.Type != ActivityType.AiSummary && a.Type != ActivityType.Break)
```

**After (SessionAIService.cs — never changes):**
```csharp
var summaryEligibleTypes = _activityRegistry.GetIncludedInAiSummary()
    .Select(p => p.ActivityType)
    .ToHashSet();

activities.Where(a => summaryEligibleTypes.Contains(a.Type))
```

Same pattern eliminates the hardcoded lists in `IntelligentSessionAIService` and `MLNetSessionAIService`.

### 3.7 Eliminating the Duplicate Enum

With a plugin registry, the `Contracts` project no longer needs its own `ActivityType` enum. The `Domain` enum is the single source of truth. `ApiMapper`'s conversion switch statements disappear entirely.

```
TechWayFit.Pulse.Contracts.Enums.ActivityType  ← DELETE
TechWayFit.Pulse.Domain.Enums.ActivityType     ← KEEP (used everywhere)
ApiMapper.MapActivityType() switch             ← DELETE (no longer needed)
ApiMapper.MapActivityType() reverse switch     ← DELETE (no longer needed)
```

### 3.8 Unifying the Dashboard Service Interfaces

**Before: 7 separate interfaces, 7 registrations, 17 constructor parameters in `SessionsController`:**
```csharp
IPollDashboardService, IWordCloudDashboardService, IRatingDashboardService,
IGeneralFeedbackDashboardService, IQnADashboardService, IQuadrantDashboardService,
IFiveWhysAIService (uses special handling)
```

**After: 1 interface:**
```csharp
public interface IActivityDashboardService
{
    Task<IActivityDashboardData> GetDashboardAsync(
        ActivityType type,
        Guid sessionId, Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken ct);
}

// Implementation delegates to the plugin registry:
public sealed class ActivityDashboardService : IActivityDashboardService
{
    public async Task<IActivityDashboardData> GetDashboardAsync(
        ActivityType type, Guid sessionId, Guid activityId,
        IReadOnlyDictionary<string, string?> filters, CancellationToken ct)
    {
        var plugin = _registry.GetPlugin(type);
        return await plugin.GetDashboardDataAsync(sessionId, activityId, filters, _dataContext, ct);
    }
}
```

`SessionsController` goes from 17 injected dependencies to ≤6.

---

## 4. Project Structure After Refactoring

### Option A: Namespace-module (start here — zero project changes)

This achieves the logical separation immediately using the existing project structure:

```
TechWayFit.Pulse.Application/
  Activities/
    Abstractions/
      IActivityPlugin.cs
      IActivityRegistry.cs
      IActivityDashboardData.cs
      IActivityDataContext.cs
    Registry/
      ActivityRegistry.cs
      ActivityDataContext.cs     ← thin adapter over existing repositories
    Dashboard/
      ActivityDashboardService.cs
    Plugins/                     ← one folder per activity
      Poll/
        PollActivityPlugin.cs    ← owns: default config, limits, dashboard data
      WordCloud/
        WordCloudActivityPlugin.cs
      Rating/
        RatingActivityPlugin.cs
      QnA/
        QnAActivityPlugin.cs
      Quadrant/
        QuadrantActivityPlugin.cs
      FiveWhys/
        FiveWhysActivityPlugin.cs
      GeneralFeedback/
        GeneralFeedbackActivityPlugin.cs
      AiSummary/
        AiSummaryActivityPlugin.cs
      Break/
        BreakActivityPlugin.cs

TechWayFit.Pulse.Web/
  Components/
    Participant/Activities/      ← existing per-activity Razor components, unchanged
    Dashboards/                  ← existing per-activity dashboard components, unchanged
    Presentation/                ← existing per-activity presentation components, unchanged
```

### Option B: Plugin project per activity (extract progressively)

Once Option A is in place, each plugin folder can be extracted into its own project:

```
TechWayFit.Pulse.Activities.Abstractions   ← interfaces, shared types
TechWayFit.Pulse.Activities.Poll           ← poll plugin + Razor components
TechWayFit.Pulse.Activities.WordCloud      ← word cloud plugin + Razor components
TechWayFit.Pulse.Activities.Rating
TechWayFit.Pulse.Activities.QnA
TechWayFit.Pulse.Activities.Quadrant
TechWayFit.Pulse.Activities.FiveWhys
TechWayFit.Pulse.Activities.GeneralFeedback
TechWayFit.Pulse.Activities.AiSummary
TechWayFit.Pulse.Activities.Break
```

Each project references only:
- `TechWayFit.Pulse.Activities.Abstractions`
- `TechWayFit.Pulse.Domain` (for entity types)
- `Microsoft.AspNetCore.Components` (for Razor components)

This makes each activity independently:
- **Buildable** and **testable** in isolation
- **Versioned** and **deployable** separately in a future plugin/extension model
- **Removable** by simply unregistering it — no other code changes needed

### Registration Pattern

```csharp
// Each plugin assembly exposes a single extension method:
// TechWayFit.Pulse.Activities.Poll/ServiceCollectionExtensions.cs
public static class PollActivityExtensions
{
    public static IServiceCollection AddPollActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, PollActivityPlugin>();
        return services;
    }
}

// Program.cs (or a dedicated AddPulseActivities extension):
builder.Services
    .AddPollActivity()
    .AddWordCloudActivity()
    .AddRatingActivity()
    .AddQnAActivity()
    .AddQuadrantActivity()
    .AddFiveWhysActivity()
    .AddGeneralFeedbackActivity()
    .AddAiSummaryActivity()
    .AddBreakActivity()
    .AddSingleton<IActivityRegistry, ActivityRegistry>();
    // ^ collects all IActivityPlugin registrations automatically via IEnumerable<IActivityPlugin>
```

If you want to disable an activity entirely (e.g., disable AiSummary if AI is not enabled), remove its registration — every dispatch chain, dashboard, component, and AI filter honours that immediately.

---

## 5. What Changes at Each Dispatch Site

| Current site | Change required | Result |
|---|---|---|
| `LiveCurrentActivity.razor` – 9 if/else | Replace with `DynamicComponent` | Never changes again |
| `Pages/Participant/Activity.razor` – 9 if/else | Replace with `DynamicComponent` | Never changes again |
| `Pages/Facilitator/Presentation.razor` – 8 if/else | Replace with `DynamicComponent` | Never changes again |
| `EditActivityModal.razor` – 7 if/else | Replace with `DynamicComponent` | Never changes again |
| `SessionAIService.cs` – 7 switch cases | Replace with registry query | Never changes again |
| `SessionAIService.cs` – exclusion filter | Replace with registry query | Never changes again |
| `IntelligentSessionAIService.cs` – exclusion | Replace with registry query | Never changes again |
| `MLNetSessionAIService.cs` – exclusion | Replace with registry query | Never changes again |
| `ApiMapper.cs` – 2 × 9 switch cases | Delete entirely | Gone |
| `IPollDashboardService` + 6 peers | Delete, replace with `IActivityDashboardService` | Unified |
| `Program.cs` – 7+ service registrations | Replace with `AddPulseActivities()` | 1 line |

---

## 6. Adding a New Activity Type — After Refactoring

**Before:** edit 22 files, touch 6 projects, follow a 24-step guide.

**After:**

1. Create `XxxActivityPlugin.cs` (implements `IActivityPlugin`) ← all config, limits, dashboard data
2. Create `XxxActivity.razor` ← participant UI
3. Create `XxxDashboard.razor` ← facilitator live view
4. Create `XxxPresentation.razor` ← presentation mode
5. Create `XxxCreateModal.razor` ← add activity modal (replaces both `.razor` + `.cshtml` files)
6. Add `ActivityType.Xxx` to the **single** `Domain.Enums.ActivityType` enum
7. Call `services.AddXxxActivity()` in `Program.cs`

That is: **7 steps, all in new files, zero changes to existing files.**

---

## 7. Architecture Risk Assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| `DynamicComponent` adds Blazor render complexity | Low | Blazor Server has supported it since .NET 6; parameters are type-checked at startup in tests |
| Plugin registry dictionary lookup has overhead | Negligible | Plugins are singletons; lookup is O(1) dictionary read |
| Dashboard data typed as `IActivityDashboardData` loses compile-time safety | Medium | Use generic `IActivityDashboardData<T>` where callers cast in Razor; add runtime type check at test boundary |
| Large-scale refactor risk to existing activities | Medium | Migrate one plugin at a time; existing code paths continue working until each `if/else` branch is replaced and the old service deleted |

---

## 8. Recommended Phased Migration

### Phase 1 — Define the Contract (1 sprint, zero risk)
1. Add `IActivityPlugin`, `IActivityRegistry`, `IActivityDataContext`, `IActivityDashboardData` interfaces
2. Implement `ActivityRegistry` backed by `IEnumerable<IActivityPlugin>`
3. Register everything — nothing else changes yet

### Phase 2 — Migrate Plugins One at a Time (1–2 sprints)
4. Create `PollActivityPlugin` — move `PollDashboardService` logic in; delete `IPollDashboardService`
5. Create `WordCloudActivityPlugin` — move `WordCloudDashboardService` logic in
6. Continue for each activity type, deleting the old service + interface each time
7. Replace `if/else` in `LiveCurrentActivity.razor` with `DynamicComponent` once all plugins exist

### Phase 3 — Eliminate All Dispatch Chains (1 sprint)
8. Replace `DynamicComponent` in remaining `.razor` files
9. Replace AI exclusion lists with registry queries
10. Delete `ApiMapper` switch statements; remove `Contracts.Enums.ActivityType` duplicate

### Phase 4 — Extract to Projects (optional, as needed)
11. Move each `*ActivityPlugin.cs` + its Razor components into a dedicated project
12. Activity project references only `Abstractions` + `Domain` + `AspNetCore.Components`

---

## 9. Summary

| Dimension | Current | After refactoring |
|---|---|---|
| Files changed per new activity | 22 | 7 (all new files) |
| Files changed per activity behaviour change | 1–8 | 1 |
| Activity types self-contained and independently testable | No | Yes |
| Can disable an activity by removing its registration | No | Yes |
| If/else dispatch chains in .razor files | 4 chains × 8 branches | 0 |
| Dashboard service interfaces | 7 separate | 1 unified |
| Program.cs DI registrations per activity | 3–4 | 1 |
| Can extract activity to its own project | No | Yes (Phase 4) |
