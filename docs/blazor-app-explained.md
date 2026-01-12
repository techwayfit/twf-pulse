# Blazor Application Architecture & Rendering Pipeline

## Table of Contents
1. [Blazor Hosting Models](#blazor-hosting-models)
2. [Blazor Server Architecture](#blazor-server-architecture)
3. [Rendering Pipeline](#rendering-pipeline)
4. [Component Lifecycle](#component-lifecycle)
5. [Interactive Rendering Modes (.NET 8+)](#interactive-rendering-modes-net-8)
6. [JavaScript Interop](#javascript-interop)
7. [Common Pitfalls & Solutions](#common-pitfalls--solutions)

---

## Blazor Hosting Models

### Blazor Server
- **Execution**: All code runs on the server
- **Connection**: Uses SignalR WebSocket for real-time UI updates
- **State**: Maintained in server memory
- **Latency**: Each UI interaction requires a round-trip to server
- **Deployment**: Requires ASP.NET Core server
- **Pros**: Small download, full .NET runtime, secure (code stays on server)
- **Cons**: Requires constant connection, server resources per user

### Blazor WebAssembly
- **Execution**: C# code runs in browser via WebAssembly
- **Connection**: Only needed for API calls
- **State**: Maintained in browser
- **Latency**: Instant UI interactions (no round-trips)
- **Deployment**: Can be static files (CDN-friendly)
- **Pros**: Works offline, scalable (client-side compute)
- **Cons**: Larger initial download, limited .NET runtime

---

## Blazor Server Architecture

```
┌─────────────────────────────────────────────────────────┐
│                        Browser                          │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │              HTML/CSS/JavaScript                 │  │
│  │  (Rendered UI + blazor.server.js client)        │  │
│  └──────────────────┬───────────────────────────────┘  │
│                     │                                   │
│                     │ SignalR WebSocket                │
└─────────────────────┼───────────────────────────────────┘
                      │
                      │ UI Events (clicks, input changes)
                      │ & DOM Updates
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    ASP.NET Core Server                  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │           Blazor Server Circuit                  │  │
│  │  - Component instances                           │  │
│  │  - Component state                               │  │
│  │  - Render tree (virtual DOM)                     │  │
│  └──────────────────┬───────────────────────────────┘  │
│                     │                                   │
│                     ▼                                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │           Rendering Engine                       │  │
│  │  1. Component method execution                   │  │
│  │  2. Diff calculation (old vs new)                │  │
│  │  3. Generate update instructions                 │  │
│  └──────────────────┬───────────────────────────────┘  │
│                     │                                   │
│                     │ DOM patches                       │
│                     └──────────────────────────────────►│
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │        Backend Services & Data Access            │  │
│  │  - EF Core DbContext                             │  │
│  │  - Business logic services                       │  │
│  │  - External APIs                                 │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Rendering Pipeline

### How Blazor Server Renders Updates

```
User clicks button
      │
      ▼
1. Browser sends event via SignalR
      │
      ▼
2. Server invokes event handler (e.g., @onclick method)
      │
      ▼
3. Component state changes
      │
      ▼
4. Component calls StateHasChanged() (automatic or manual)
      │
      ▼
5. Render tree comparison (diffing)
      │
      ├─ Old Render Tree (cached)
      └─ New Render Tree (from re-rendering component)
      │
      ▼
6. Calculate minimal DOM changes
      │
      ▼
7. Send DOM patches via SignalR
      │
      ▼
8. Browser applies changes to actual DOM
```

### Pre-rendering vs Interactive Rendering

#### Pre-rendering (Default in .NET 8)
```html
<!-- Server generates static HTML -->
<div>Hello, World!</div>
```
- Happens on **first request** (server-side)
- Generates static HTML sent to browser
- Fast initial page load
- Good for SEO
- **Not interactive yet**

#### Interactive Rendering
```html
<!-- After SignalR connects, components become interactive -->
<div>Hello, World!</div>
<!-- Now @onclick works -->
```
- Happens **after** SignalR connection established
- Component becomes "live"
- User interactions trigger server methods
- State synchronized

---

## Component Lifecycle

### Lifecycle Methods (in order)

```csharp
public class MyComponent : ComponentBase
{
    // 1. Constructor
    public MyComponent()
    {
        // Component instance created
        // ⚠️ Services not available yet
    }

    // 2. SetParametersAsync (before each render)
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        // Parameters have been set
        // Can perform logic based on parameter changes
    }

    // 3. OnInitialized (once)
    protected override void OnInitialized()
    {
        // Component initialized
        // ✅ Services available
        // ⚠️ NOT rendered yet (no DOM)
    }

    // 4. OnInitializedAsync (once, async version)
    protected override async Task OnInitializedAsync()
    {
        // Async initialization
        // Load data from API, database, etc.
    }

    // 5. OnParametersSet (after parameters set)
    protected override void OnParametersSet()
    {
        // Called after parameters are set
        // Happens after initial render and on parameter changes
    }

    // 6. OnParametersSetAsync (async version)
    protected override async Task OnParametersSetAsync()
    {
        // Async parameter processing
    }

    // 7. OnAfterRender (after each render)
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ DOM exists now
            // Safe to call JavaScript interop
        }
    }

    // 8. OnAfterRenderAsync (async version)
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize JavaScript components
            await JS.InvokeVoidAsync("initFormBuilder", "myElement");
        }
    }

    // 9. ShouldRender (controls re-rendering)
    protected override bool ShouldRender()
    {
        // Return false to prevent re-render
        // Use for performance optimization
        return true;
    }

    // 10. Dispose
    public void Dispose()
    {
        // Cleanup: remove event handlers, dispose services
    }
}
```

### When Each Method Runs

```
Page Load:
  Constructor → SetParametersAsync → OnInitialized → OnInitializedAsync 
  → OnParametersSet → OnParametersSetAsync → [RENDER] 
  → OnAfterRender(firstRender: true) → OnAfterRenderAsync(firstRender: true)

Parameter Change:
  SetParametersAsync → OnParametersSet → OnParametersSetAsync 
  → [RENDER] → OnAfterRender(firstRender: false) → OnAfterRenderAsync(firstRender: false)

State Change (via StateHasChanged):
  [RENDER] → OnAfterRender(firstRender: false) → OnAfterRenderAsync(firstRender: false)

Component Removed:
  Dispose
```

---

## Interactive Rendering Modes (.NET 8+)

### Render Mode Options

#### 1. Static Server Rendering (Default)
```razor
@page "/static-page"

<h1>Static Content</h1>
<!-- Pre-rendered HTML only, no interactivity -->
```
- No SignalR connection
- No `@onclick`, `@bind` support
- Fast, SEO-friendly
- Use for read-only pages

#### 2. Interactive Server
```razor
@page "/interactive-page"
@rendermode InteractiveServer

<button @onclick="HandleClick">Click Me</button>

@code {
    private void HandleClick()
    {
        // This works! SignalR active
    }
}
```
- Full Blazor Server capabilities
- SignalR connection required
- Use for interactive pages

#### 3. Interactive WebAssembly
```razor
@rendermode InteractiveWebAssembly
```
- Runs in browser (client-side)
- No server connection needed

#### 4. Interactive Auto
```razor
@rendermode InteractiveAuto
```
- Starts as Server, downloads WebAssembly in background
- Switches to WebAssembly when ready

### Per-Component Render Modes

```razor
<!-- Parent: Static -->
@page "/mixed"

<h1>Static Content</h1>

<!-- Child: Interactive -->
<Counter @rendermode="InteractiveServer" />

<!-- This section is static again -->
<footer>Footer content</footer>
```

**Important**: Interactive components can't have static children directly.

---

## JavaScript Interop

### Calling JavaScript from C#

```csharp
// Inject IJSRuntime
@inject IJSRuntime JS

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ Safe: DOM exists
            await JS.InvokeVoidAsync("myJsFunction", "arg1", "arg2");
            
            var result = await JS.InvokeAsync<string>("myJsFunction");
        }
    }
}
```

### JavaScript Function
```javascript
// wwwroot/js/myfile.js
window.myJsFunction = function(arg1, arg2) {
    console.log('Called from Blazor:', arg1, arg2);
    return "result";
};
```

### Calling C# from JavaScript

```csharp
[JSInvokable]
public static Task<string> GetServerData()
{
    return Task.FromResult("Data from server");
}
```

```javascript
DotNet.invokeMethodAsync('AssemblyName', 'GetServerData')
    .then(data => console.log(data));
```

---

## Common Pitfalls & Solutions

### Problem 1: JavaScript-Modified DOM Gets Wiped

**Symptom**: You use JavaScript to add/modify DOM elements, but they disappear on re-render.

**Why**: Blazor's diff algorithm replaces the DOM section when component re-renders.

**Solution**:
```razor
<!-- Option A: Use @key to preserve DOM element -->
<div @key="stableId" id="jsContainer">
    <!-- JavaScript modifies this -->
</div>

<!-- Option B: Move JavaScript section to separate non-interactive component -->
<StaticJsComponent />  <!-- Not marked with @rendermode -->
```

```csharp
// Option C: Prevent re-renders when not needed
protected override bool ShouldRender()
{
    return needsUpdate; // Control when to re-render
}
```

### Problem 2: SignalR Connection Errors

**Symptom**: 
```
Connection closed with error
Server returned an error on close
```

**Common Causes**:
1. Version mismatch: `Microsoft.AspNetCore.SignalR.Client` version doesn't match .NET version
2. Missing `@rendermode` when using `@bind` or `@onclick`
3. Loading `signalr.min.js` manually (conflicts with blazor.server.js)

**Solution**:
```xml
<!-- Use matching version -->
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
```

```razor
<!-- Add render mode for interactive features -->
@rendermode InteractiveServer
```

### Problem 3: Parameters Not Updating

**Symptom**: Child component doesn't update when parent changes parameter.

**Why**: Blazor only re-renders if it detects the parameter reference changed.

**Solution**:
```csharp
// ❌ Wrong: Modifying object properties doesn't trigger update
public class MyData 
{
    public string Name { get; set; }
}
myData.Name = "New Name"; // Child won't update

// ✅ Right: Create new instance
myData = new MyData { Name = "New Name" };

// ✅ Or use EventCallback
[Parameter] public EventCallback<MyData> OnDataChanged { get; set; }
await OnDataChanged.InvokeAsync(myData);
```

### Problem 4: Form Builder Disappearing (Your Case)

**Root Cause**: 
1. Parent page has interactive child (`SessionContextTabs`)
2. When child re-renders (tab switch), parent also re-renders
3. Parent re-render wipes out JavaScript-generated form builder DOM

**Solution**:
```razor
<!-- Isolate interactive section -->
<SessionContextTabs Data="model" @rendermode="InteractiveServer" />

<!-- Keep static section with @key for stability -->
<div @key="formBuilderKey">
    <div id="joinFormBuilder">
        <!-- JavaScript modifies this -->
    </div>
</div>
```

```csharp
private readonly string formBuilderKey = Guid.NewGuid().ToString();

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Initialize only once
        await JS.InvokeVoidAsync("initFormBuilder", "joinFormBuilder");
    }
}
```

### Problem 5: Memory Leaks

**Symptom**: Server memory grows over time.

**Why**: Event handlers, timers, or SignalR subscriptions not disposed.

**Solution**:
```csharp
public class MyComponent : ComponentBase, IDisposable
{
    private Timer? _timer;
    
    protected override void OnInitialized()
    {
        _timer = new Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
    }
    
    public void Dispose()
    {
        _timer?.Dispose(); // ✅ Clean up
    }
}
```

---

## Best Practices

### 1. Minimize Re-renders
```csharp
// Only re-render when necessary
protected override bool ShouldRender()
{
    return _dataChanged;
}
```

### 2. Use ViewModel Pattern
```csharp
// Instead of many parameters
[Parameter] public SessionViewModel Data { get; set; }

// Easier to manage, fewer event callbacks
```

### 3. Proper Async Handling
```csharp
// ❌ Wrong: Blocking
var data = _service.GetData().Result;

// ✅ Right: Async
var data = await _service.GetDataAsync();
```

### 4. JavaScript Interop Timing
```csharp
// ❌ Wrong: DOM doesn't exist yet
protected override void OnInitialized()
{
    await JS.InvokeVoidAsync("initElement"); // ERROR
}

// ✅ Right: DOM exists
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await JS.InvokeVoidAsync("initElement"); // OK
    }
}
```

### 5. Use @key for Dynamic Lists
```razor
@foreach (var item in items)
{
    <!-- Helps Blazor track which items changed -->
    <ItemComponent @key="item.Id" Data="item" />
}
```

### 6. Scoped Services in Blazor Server
```csharp
// Services are scoped to SignalR circuit (per-user session)
builder.Services.AddScoped<IMyService, MyService>();

// State persists across page navigations for same user
```

---

## Debugging Tips

### 1. Enable Detailed Logging
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```

### 2. Browser Console
- Check for JavaScript errors
- Look for SignalR connection messages
- Monitor network tab for WebSocket traffic

### 3. Add Console Logging
```csharp
protected override void OnInitialized()
{
    Console.WriteLine($"Component initialized: {GetType().Name}");
}

protected override void OnAfterRender(bool firstRender)
{
    Console.WriteLine($"Rendered: firstRender={firstRender}");
}
```

---

## Summary

### Blazor Server Flow
1. **Request** → Server generates pre-rendered HTML
2. **Browser** → Loads HTML + blazor.server.js
3. **SignalR** → Establishes WebSocket connection
4. **Circuit** → Server creates component instances
5. **Interactive** → User interactions trigger server methods
6. **Diff & Patch** → Minimal DOM updates sent to browser

### Key Takeaways
- **Pre-rendering** ≠ **Interactive rendering**
- Use `OnAfterRenderAsync(firstRender)` for JavaScript interop
- `@key` preserves elements across re-renders
- Proper lifecycle method usage is critical
- Always dispose resources to prevent leaks
- Match package versions to .NET version

---

## Further Reading
- [Official Blazor Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Blazor University](https://blazor-university.com/)
- [Awesome Blazor](https://github.com/AdrienTorris/awesome-blazor)
