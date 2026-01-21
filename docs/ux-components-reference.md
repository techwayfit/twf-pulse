# UX Components Quick Reference Guide

## Stat Cards

### Usage
```html
<!-- Primary (Blue) -->
<div class="stat-card stat-card-primary">
  <div class="stat-card-body">
    <div class="stat-card-content">
      <div class="stat-card-label">Total Sessions</div>
      <div class="stat-card-value">42</div>
    </div>
    <div class="stat-card-icon text-primary">📊</div>
  </div>
</div>

<!-- Success (Green) -->
<div class="stat-card stat-card-success">...</div>

<!-- Warning (Amber) -->
<div class="stat-card stat-card-warning">...</div>

<!-- Secondary (Gray) -->
<div class="stat-card stat-card-secondary">...</div>

<!-- Empty State (Gray with message) -->
<div class="stat-card stat-card-empty">...</div>
```

### Features
- ✨ Gradient background based on variant
- 🎨 3px top accent line on hover
- 📈 Lift animation (4px translateY)
- 💫 Icon scale on hover
- 🎯 Empty state encouragement message

---

## Status Badges

### Usage
```html
<!-- Live (Green with pulsing dot) -->
<span class="status-badge status-live">Live</span>

<!-- Draft (Amber) -->
<span class="status-badge status-draft">Draft</span>

<!-- Ended (Gray) -->
<span class="status-badge status-ended">Ended</span>

<!-- Expired (Red) -->
<span class="status-badge status-expired">Expired</span>
```

### Features
- ✨ Gradient backgrounds
- 🔴 Pulsing dot on "Live" status
- 💫 Glow animation on "Live"
- 🎨 Fully rounded pill shape
- 📏 Consistent sizing and padding

---

## Buttons

### Primary Button
```html
<button class="btn btn-pulse-primary">
  Primary Action
</button>

<!-- With lift effect -->
<button class="btn btn-pulse-primary btn-lift">
  Primary with Lift
</button>

<!-- Loading state -->
<button class="btn btn-pulse-primary btn-loading">
  Loading...
</button>

<!-- Disabled -->
<button class="btn btn-pulse-primary" disabled>
  Disabled
</button>
```

### Outline Button
```html
<button class="btn btn-pulse-outline">
  Secondary Action
</button>
```

### Icon Button
```html
<button class="btn btn-icon btn-primary">
  <i class="fas fa-heart"></i>
</button>
```

### Features
- ✨ Ripple effect on click (primary)
- 📈 Lift on hover
- 🎯 Focus-visible outline
- ⏳ Loading spinner state
- 🚫 Proper disabled state

---

## Form Controls

### Enhanced Input
```html
<div class="mb-3">
  <label class="form-label">Email Address</label>
  <input type="email" class="form-control" placeholder="you@example.com">
  <div class="form-text">We'll never share your email.</div>
</div>
```

### With Validation
```html
<!-- Valid -->
<input type="text" class="form-control is-valid">
<div class="valid-feedback">Looks good!</div>

<!-- Invalid -->
<input type="text" class="form-control is-invalid">
<div class="invalid-feedback">Please provide a valid input.</div>
```

### Textarea with Character Counter
```html
<div class="mb-3">
  <label class="form-label">Description</label>
  <textarea class="form-control" maxlength="200"></textarea>
  <div class="character-counter">
    <span class="current">0</span> / <span class="max">200</span>
  </div>
</div>

<!-- Add classes for limits -->
<div class="character-counter near-limit">180 / 200</div>
<div class="character-counter at-limit">200 / 200</div>
```

### Toggle Switch
```html
<div class="form-check form-switch">
  <input class="form-check-input" type="checkbox" id="toggleSwitch">
  <label class="form-check-label" for="toggleSwitch">
    Enable feature
  </label>
</div>
```

### Features
- ✨ Blue glow ring on focus
- 🎨 Hover state before focus
- ✅ Validation icons
- 📊 Character counters
- 🔄 Modern toggle switches
- 📏 Consistent sizing

---

## Form Sections

### Usage
```html
<div class="form-section">
  <h5 class="form-section-title">Section Title</h5>
  
  <div class="mb-3">
    <label class="form-label">Field 1</label>
    <input type="text" class="form-control">
  </div>
  
  <div class="mb-3">
    <label class="form-label">Field 2</label>
    <input type="text" class="form-control">
  </div>
</div>
```

### Features
- 🎨 Light background
- 📦 Grouped content
- 📏 Consistent padding
- 🔖 Title with bottom border

---

## Empty States

### Usage
```html
<div class="empty-state">
  <div class="empty-state-icon">
    <svg width="80" height="80">
      <!-- Your SVG icon -->
    </svg>
  </div>
  <h3 class="empty-state-title">No Items Found</h3>
  <p class="empty-state-text">
    You haven't created any items yet. Get started by creating your first one!
  </p>
  <a href="/create" class="btn btn-pulse-primary btn-lift">
    ✨ Create Your First Item
  </a>
</div>
```

### Features
- 🎨 Dashed border
- 💫 Floating icon animation
- ✨ Pulsing CTA button
- 📝 Clear messaging
- 🎯 Centered layout

---

## Micro-Interactions

### Lift Effect
```html
<button class="btn btn-primary btn-lift">Lift on Hover</button>
<div class="card btn-lift">Card lifts too</div>
```

### Loading State
```html
<button class="btn btn-primary btn-loading">
  <!-- Text becomes invisible, spinner shows -->
  Loading...
</button>
```

### Fade In Animation
```html
<div class="card fade-in">
  Content fades in smoothly
</div>
```

### Shake Animation (Error Feedback)
```html
<div class="alert alert-danger shake">
  Error message shakes to grab attention
</div>
```

### Success Checkmark
```html
<span class="badge bg-success success-checkmark">
  Saved!
</span>
```

---

## Tables

### Enhanced Table Rows
```html
<table class="sessions-table">
  <thead>
    <tr>
      <th>Column 1</th>
      <th>Column 2</th>
    </tr>
  </thead>
  <tbody>
    <!-- Automatic hover effects -->
    <tr>
      <td>Data 1</td>
      <td>Data 2</td>
    </tr>
  </tbody>
</table>
```

### Features
- ✨ Left accent border on hover
- 🎨 Gradient background on hover
- 📈 2px slide animation
- 💡 Action button highlights

---

## Utility Classes

### Shadows
```html
<div class="shadow-xs">Extra Small Shadow</div>
<div class="shadow-sm">Small Shadow</div>
<div class="shadow-md">Medium Shadow</div>
<div class="shadow-lg">Large Shadow</div>
<div class="shadow-xl">Extra Large Shadow</div>
<div class="shadow-glow">Blue Glow Effect</div>
```

### Glass Effect
```html
<div class="glass">
  Glassmorphism background
</div>
```

### Gradient Text
```html
<h1 class="gradient-text">
  Blue to Mint Gradient Text
</h1>
```

---

## Color Variables

### Available CSS Variables
```css
/* Brand Colors */
--pulse-bg: #FAFBFC
--pulse-blue: #0066FF
--pulse-mint: #00D9A3
--pulse-teal: #00C9B7
--pulse-purple: #7C3AED
--pulse-amber: #F59E0B

/* Neutrals */
--pulse-dark: #0F172A
--pulse-gray: #64748B
--pulse-light: #F8FAFC

/* Semantic */
--pulse-success: #10B981
--pulse-warning: #F59E0B
--pulse-danger: #EF4444

/* Shadows */
--shadow-xs through --shadow-xl
--shadow-glow

/* Border Radius */
--radius-xs: 6px
--radius-sm: 10px
--radius-md: 14px
--radius-lg: 20px
--radius-xl: 28px
--radius-full: 9999px
```

---

## Best Practices

### DO ✅
- Use Bootstrap classes first
- Add custom classes only when needed
- Use semantic HTML
- Include proper ARIA labels
- Test keyboard navigation
- Ensure color contrast
- Keep animations subtle

### DON'T ❌
- Override Bootstrap utility classes
- Use inline styles
- Use `!important` unless necessary
- Remove focus indicators
- Create page-specific global styles
- Use ID selectors for styling
- Disable animations without fallback

---

## Browser Support

### Fully Supported ✅
- Chrome/Edge (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- iOS Safari (latest 2 versions)
- Chrome Mobile (latest)

### Features with Fallbacks
- `backdrop-filter` (degrades gracefully)
- CSS Grid (fallback to flexbox where needed)
- CSS Custom Properties (fallback values provided)

---

## Performance Tips

1. **Animations:** All use CSS transforms (GPU-accelerated)
2. **No JavaScript:** Pure CSS solutions
3. **Minimal Repaints:** Avoid layout thrashing
4. **Cached CDN:** Bootstrap loaded from CDN
5. **Progressive Enhancement:** Works without JavaScript

---

## Accessibility Checklist

- ✅ Focus-visible states on all interactive elements
- ✅ Color contrast meets WCAG AA
- ✅ Keyboard navigation supported
- ✅ Screen reader friendly
- ✅ Touch targets ≥44px on mobile
- ✅ No motion for critical functionality
- ⚠️ Add `prefers-reduced-motion` (recommended)

---

## Testing Commands

```bash
# Run development server
cd src/TechWayFit.Pulse.Web
dotnet watch

# Build for production
dotnet build --configuration Release

# Run tests
dotnet test
```

---

## Quick Links

- [Full Documentation](./ux-improvements-implemented.md)
- [Project README](../README.md)
- [Bootstrap 5.3 Docs](https://getbootstrap.com/docs/5.3/)
- [Copilot Instructions](../.github/copilot-instructions.md)

---

**Last Updated:** January 21, 2026  
**Version:** 1.0.0
