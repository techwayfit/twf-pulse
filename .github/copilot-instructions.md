# GitHub Copilot Instructions for TechWayFit Pulse

## Project Overview
TechWayFit Pulse is a .NET 8 Blazor Server application for running interactive workshops with real-time activities like polls, word clouds, and quadrant matrices.

## Technology Stack
- **Backend**: .NET 8, ASP.NET Core, Blazor Server
- **Database**: SQLite (with in-memory option for development)
- **Frontend**: Blazor Server, Bootstrap 5.3, Vanilla JavaScript
- **Real-time**: SignalR
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, Web)

## Coding Standards & Best Practices

### 1. CSS & Styling
**?? IMPORTANT: Bootstrap-First Approach**

- **Always use Bootstrap 5.3 classes first** before writing custom CSS
- Only create custom CSS when Bootstrap cannot achieve the desired result
- Reference: Bootstrap is already included via CDN in `_Layout.cshtml`
- Always create custom generic class when needed. avoid inline styles , !important, and id selectors. avoid page specific styles in global css files.

#### Bootstrap Usage Examples:
```html
<!-- ? CORRECT: Use Bootstrap classes -->
<div class="card shadow-sm mb-4">
  <div class="card-body">
    <h5 class="card-title">Title</h5>
    <p class="card-text text-muted">Description</p>
  </div>
</div>

<!-- ? WRONG: Don't create custom classes for standard layouts -->
<div class="custom-card custom-shadow custom-margin">
  <div class="custom-body">
    <h5 class="custom-title">Title</h5>
  </div>
</div>
```

#### Bootstrap Classes to Prefer:
- **Layout**: `.container`, `.container-fluid`, `.row`, `.col-*`
- **Spacing**: `.m-*`, `.p-*`, `.gap-*`, `.g-*` (instead of custom margins/padding)
- **Flexbox**: `.d-flex`, `.justify-content-*`, `.align-items-*`
- **Typography**: `.h1`-`.h6`, `.text-*`, `.fw-*`, `.fs-*`
- **Components**: `.card`, `.btn`, `.form-control`, `.alert`, `.badge`
- **Utilities**: `.shadow-sm`, `.rounded`, `.border`, `.bg-*`

#### When to Use Custom CSS:
Only create custom CSS for:
1. Unique drag-and-drop functionality
2. Custom animations not in Bootstrap
3. Brand-specific colors/themes
4. Complex interactive components (e.g., form builder)

### 2. Blazor vs MVC

**Static Pages (No Interactivity):**
- Use **MVC** (Controllers + Views)
- No WebSocket overhead
- Better for SEO and performance
- Examples: Homepage, Create Session form

**Interactive Pages (Real-time Features):**
- Use **Blazor Server**
- Leverage SignalR for real-time updates
- Examples: Live facilitator view, Participant activity view

### 3. File Organization

```
src/TechWayFit.Pulse.Web/
??? Controllers/           # MVC Controllers for static pages
?   ??? HomeController.cs
? ??? Api/   # API Controllers
??? Views/                # MVC Views (static pages)
?   ??? Home/
?   ??? Shared/
??? Pages/       # Blazor Pages (interactive)
?   ??? Facilitator/
?   ??? Participant/
??? wwwroot/
?   ??? css/
?   ?   ??? pulse-ui.css       # Global design system
?   ?   ??? create-session.css # Page-specific (minimal custom CSS)
?   ??? js/
?       ??? *.js      # Vanilla JavaScript (no jQuery)
```

### 4. Naming Conventions

**C# Code:**
- Classes: `PascalCase`
- Methods: `PascalCase`
- Private fields: `_camelCase`
- Properties: `PascalCase`
- Local variables: `camelCase`

**CSS:**
- Custom classes: `kebab-case` (e.g., `.form-builder-container`)
- Bootstrap classes: Use as-is (e.g., `.d-flex`, `.mb-3`)

**JavaScript:**
- Variables/Functions: `camelCase`
- Classes: `PascalCase`
- Constants: `UPPER_SNAKE_CASE`

### 5. Dependency Injection

Always use constructor injection:
```csharp
// ? CORRECT
public class SessionService
{
    private readonly ISessionRepository _repository;
    
    public SessionService(ISessionRepository repository)
    {
        _repository = repository;
    }
}

// ? WRONG: Don't use service locator pattern
var service = serviceProvider.GetService<ISessionService>();
```

### 6. API Design

**RESTful Routes:**
```csharp
// ? CORRECT
[HttpGet]
public async Task<ActionResult<SessionResponse>> GetSession(string code) { }

[HttpPost]
public async Task<ActionResult<SessionResponse>> CreateSession([FromBody] CreateSessionRequest request) { }

// ? WRONG: Don't use verbs in route names
[HttpGet("GetSessionByCode")]
public async Task<ActionResult<SessionResponse>> GetSessionByCode(string code) { }
```

### 7. Error Handling

```csharp
// ? CORRECT: Return appropriate HTTP status codes
try
{
    var session = await _service.GetSessionAsync(code);
 if (session == null)
        return NotFound(new { message = "Session not found" });
    
    return Ok(session);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to get session {Code}", code);
    return StatusCode(500, new { message = "Internal server error" });
}

// ? WRONG: Don't swallow exceptions
catch { return null; }
```

### 8. Async/Await

```csharp
// ? CORRECT: Use async all the way
public async Task<Session> GetSessionAsync(string code)
{
    return await _dbContext.Sessions
   .Include(s => s.Activities)
        .FirstOrDefaultAsync(s => s.Code == code);
}

// ? WRONG: Don't block async calls
public Session GetSession(string code)
{
    return GetSessionAsync(code).Result; // Deadlock risk!
}
```

### 9. Entity Framework

**Use projection for DTOs:**
```csharp
// ? CORRECT: Project to DTO in query
var sessions = await _dbContext.Sessions
.Select(s => new SessionSummaryDto
    {
        Code = s.Code,
        Title = s.Title
    })
    .ToListAsync();

// ? WRONG: Load full entities then map
var sessions = await _dbContext.Sessions.ToListAsync();
var dtos = sessions.Select(s => MapToDto(s)).ToList();
```

### 10. JavaScript

**Use vanilla JavaScript (no jQuery):**
```javascript
// ? CORRECT
document.addEventListener('DOMContentLoaded', () => {
    const button = document.getElementById('myButton');
    button.addEventListener('click', handleClick);
});

// ? WRONG
$(document).ready(function() {
    $('#myButton').click(handleClick);
});
```

### 11. Configuration

**Use strongly-typed configuration:**
```csharp
// ? CORRECT
var useInMemory = builder.Configuration.GetValue<bool>("Pulse:UseInMemory");

// ? WRONG
var useInMemory = bool.Parse(builder.Configuration["Pulse:UseInMemory"]);
```

### 12. Security

- Never store secrets in code or configuration files
- Use HTTPS in production
- Validate all user input
- Use parameterized queries (EF Core handles this)
- Sanitize output to prevent XSS

## Common Patterns

### Creating a New Page

**Static Page (MVC):**
1. Create Controller in `/Controllers/`
2. Create View in `/Views/ControllerName/`
3. Use Bootstrap classes for layout
4. Add minimal custom CSS only if needed

**Interactive Page (Blazor):**
1. Create `.razor` file in `/Pages/`
2. Add `@page` directive
3. Use `@rendermode InteractiveServer` if needed
4. Leverage SignalR for real-time features

### Adding a New Feature

1. **Domain**: Add entity in `Domain/Entities/`
2. **Application**: Add service interface in `Application/Abstractions/Services/`
3. **Infrastructure**: Implement repository/service in `Infrastructure/`
4. **Contracts**: Add request/response DTOs in `Contracts/`
5. **Web**: Create controller or Blazor component
6. **DI**: Register services in `Program.cs`

## Performance Considerations

1. **Blazor**: Use `@key` for list rendering
2. **EF Core**: Use `.AsNoTracking()` for read-only queries
3. **SignalR**: Limit message size and frequency
4. **CSS**: Minimize custom CSS, leverage Bootstrap's cached files

## Testing

- Unit tests in `TechWayFit.Pulse.Tests/`
- Use xUnit
- Mock dependencies with interfaces
- Test business logic, not infrastructure

## Questions?

If unsure about any pattern or approach:
1. Check existing code for similar implementations
2. Prefer Bootstrap over custom CSS
3. Use MVC for static pages, Blazor for interactive features
4. Follow .NET best practices and SOLID principles
