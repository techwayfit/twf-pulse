# Quick Migration Example - Emoji to SVG Icons

## Example 1: QuickActionsGrid Component

### Before (with emojis):
```csharp
Icon = "🚀", Label = "Create Session"
Icon = "📊", Label = "View Dashboard"
Icon = "👥", Label = "Join Session"
Icon = "📚", Label = "Getting Started"
```

### After (with SVG paths):
```csharp
Icon = "/images/icons/rocket.svg", Label = "Create Session"
Icon = "/images/icons/chart.svg", Label = "View Dashboard"
Icon = "/images/icons/people.svg", Label = "Join Session"
Icon = "/images/icons/books.svg", Label = "Getting Started"
```

### View Template Update:
```html
<!-- Old way (shows emoji as text) -->
<span>@Model.Icon</span>

<!-- New way (shows colorful SVG) -->
<img src="@Model.Icon" alt="@Model.Label" class="action-icon" />
```

## Example 2: FeatureHighlights Component

### Before:
```csharp
new FeatureHighlightItemViewModel
{
    Icon = "🎯",
    Title = "Real-Time Engagement",
    Description = "See responses as they happen with live dashboards."
}
```

### After:
```csharp
new FeatureHighlightItemViewModel
{
    Icon = "/images/icons/target.svg",
    Title = "Real-Time Engagement",
    Description = "See responses as they happen with live dashboards."
}
```

### View Template:
```html
<div class="feature-card">
    <img src="@item.Icon" alt="" class="feature-icon mb-3" />
    <h4>@item.Title</h4>
    <p>@item.Description</p>
</div>
```

## Available Icons (Ready to Use)

| Filename | Path | Use For |
|----------|------|---------|
| `rocket.svg` | `/images/icons/rocket.svg` | Create, Launch, Start |
| `chart.svg` | `/images/icons/chart.svg` | Analytics, Dashboard, Data |
| `people.svg` | `/images/icons/people.svg` | Team, Users, Participants |
| `books.svg` | `/images/icons/books.svg` | Documentation, Learn, Resources |
| `target.svg` | `/images/icons/target.svg` | Goals, Accuracy, Focus |
| `robot.svg` | `/images/icons/robot.svg` | AI, Automation, Smart |
| `search.svg` | `/images/icons/search.svg` | Find, Explore, Search |
| `pin.svg` | `/images/icons/pin.svg` | Highlight, Important, Pin |

## CSS (Add to pulse-ui.css)

```css
/* Icon sizing */
.icon-sm { width: 16px; height: 16px; }
.icon-md { width: 24px; height: 24px; }
.icon-lg { width: 32px; height: 32px; }
.icon-xl { width: 48px; height: 48px; }

/* Specific use cases */
.feature-icon {
    width: 48px;
    height: 48px;
    display: block;
}

.action-icon {
    width: 20px;
    height: 20px;
    vertical-align: middle;
    margin-right: 0.5rem;
}
```

## Migration Strategy

1. **Phase 1 - New Components**: Use SVG icons for all new promotional/feature components
2. **Phase 2 - High Visibility**: Update homepage, landing pages first
3. **Phase 3 - Gradual**: Update existing components as needed
4. **Keep FA for**: Navigation, buttons, standard UI elements

## Testing

After updating:
```bash
dotnet build
dotnet watch
```

Navigate to `/Home/Components` to see the rendered examples.
