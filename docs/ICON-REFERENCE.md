# TechWayFit Pulse - Icon Reference

This document maps Unicode emojis to their colorful Noto SVG equivalents.

## Icon Location
All SVG icons are stored in: `/wwwroot/images/icons/`

## Emoji to SVG Mapping

| Emoji | Description | SVG File | Usage |
|-------|-------------|----------|-------|
| 🚀 | Rocket | `rocket.svg` | Create Session, Launch, Start |
| 📊 | Bar Chart | `chart.svg` | Dashboard, Analytics, Data |
| 👥 | People | `people.svg` | Participants, Team, Users |
| 📚 | Books | `books.svg` | Documentation, Getting Started, Resources |
| 🎯 | Target | `target.svg` | Goals, Objectives, Real-time Engagement |
| 🤖 | Robot | `robot.svg` | AI-Powered, Automation |
| 🔍 | Magnifying Glass | `search.svg` | Search, Find, Explore |
| 📌 | Pushpin | `pin.svg` | Pin, Highlight, Important |

## Usage Examples

### In Razor Views (.cshtml)
```html
<!-- Basic usage -->
<img src="~/images/icons/rocket.svg" alt="Create Session" class="icon" />

<!-- With inline style -->
<img src="~/images/icons/chart.svg" alt="Dashboard" style="width: 24px; height: 24px;" />

<!-- With CSS class -->
<img src="~/images/icons/robot.svg" alt="AI" class="feature-icon" />
```

### In View Models
Replace emoji strings with SVG paths:

**Before:**
```csharp
public class QuickActionItemViewModel
{
    public string Icon { get; set; } = "🚀";
    public string Label { get; set; } = "Create Session";
}
```

**After:**
```csharp
public class QuickActionItemViewModel
{
    public string Icon { get; set; } = "/images/icons/rocket.svg";
    public string Label { get; set; } = "Create Session";
}
```

### In Component Views
```html
<!-- When Icon is a path -->
@if (Model.Icon.EndsWith(".svg"))
{
    <img src="@Model.Icon" alt="@Model.Label" class="action-icon" />
}
else
{
    <span>@Model.Icon</span>
}
```

## Recommended CSS

Add to your global styles:

```css
/* Icon sizing utilities */
.icon-sm {
    width: 16px;
    height: 16px;
}

.icon-md {
    width: 24px;
    height: 24px;
}

.icon-lg {
    width: 32px;
    height: 32px;
}

.icon-xl {
    width: 48px;
    height: 48px;
}

/* Feature icons (for promotional components) */
.feature-icon {
    width: 48px;
    height: 48px;
    display: inline-block;
}

/* Action icons (for buttons/links) */
.action-icon {
    width: 20px;
    height: 20px;
    vertical-align: middle;
    margin-right: 8px;
}
```

## Adding More Icons

If you need additional Noto emoji icons:

1. Find the emoji Unicode code point (e.g., 💡 = U+1F4A1)
2. Convert to filename format: `emoji_u1f4a1.svg`
3. Copy from `docs/svg/` to `wwwroot/images/icons/`
4. Rename to descriptive name (e.g., `lightbulb.svg`)
5. Update this reference document

## Icon Source

All icons are from Google's Noto Emoji collection (Apache License 2.0).
Source: https://github.com/googlefonts/noto-emoji

## Notes

- **SVG Benefits**: Scalable, colorful, smaller file size than PNG
- **Performance**: These are optimized SVGs from Noto (2-6KB each)
- **Accessibility**: Always include meaningful `alt` text
- **Consistency**: Use the same icon for the same concept across the app
