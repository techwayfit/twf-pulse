# UI Component Library — TechWayFit Pulse

> Last Updated: February 2026 | Bootstrap 5.3

---

## 1. Design Principles

- **Bootstrap-first**: Always use Bootstrap 5.3 utility classes before writing custom CSS
- **No inline styles**: Use CSS classes defined in `wwwroot/css/pulse.css`
- **No `!important`**: Avoid overriding specificity with `!important`
- **No ID selectors** for styling — use classes only
- **No unicode emojis** — use Font Awesome icons (`fas fa-*`) or SVG icon CSS utilities (`ics ics-*`)
- **No page-specific styles in global CSS** — additions to `pulse.css` must be generic/reusable
- **Mobile-first**: All components work on small screens without modification

---

## 2. Page Navigation Structure

| Page | Technology | Route | Description |
|------|-----------|-------|-------------|
| Home | MVC | `/home` | Homepage with templates and getting started |
| Create Session | MVC | `/facilitator/create-workshop` | 4-step session creation wizard |
| Edit Session | MVC | `/facilitator/edit/{code}` | Edit session settings and activities |
| Live Console | Blazor Server | `/facilitator/live` | Live facilitator console with real-time controls |
| Dashboards | Blazor Server | `/facilitator/dashboards` | Real-time activity dashboards |
| Participant Join | MVC | `/participant/join` | Code entry + dynamic join form |
| Participant Activity | Blazor Server | `/participant/activity` | Active activity input UI |
| Participant Done | MVC | `/participant/done` | Thank you screen |

---

## 3. CSS Variables (Design Tokens)

Defined in `wwwroot/css/pulse.css`:

```css
/* Brand Colors */
--pulse-bg: #FAFBFC;
--pulse-blue: #0066FF;
--pulse-mint: #00D9A3;
--pulse-teal: #00C9B7;
--pulse-purple: #7C3AED;
--pulse-amber: #F59E0B;

/* Neutrals */
--pulse-dark: #0F172A;
--pulse-gray: #64748B;
--pulse-light: #F8FAFC;

/* Semantic */
--pulse-success: #10B981;
--pulse-warning: #F59E0B;
--pulse-danger: #EF4444;

/* Border Radius */
--radius-xs: 6px;   --radius-sm: 10px;  --radius-md: 14px;
--radius-lg: 20px;  --radius-xl: 28px;  --radius-full: 9999px;
```

---

## 4. Stat Cards

```html
<!-- Primary (blue) -->
<div class="stat-card stat-card-primary">
  <div class="stat-card-body">
    <div class="stat-card-content">
      <div class="stat-card-label">Total Sessions</div>
      <div class="stat-card-value">42</div>
    </div>
    <div class="stat-card-icon text-primary">
      <i class="fas fa-calendar"></i>
    </div>
  </div>
</div>

<!-- Variants: stat-card-success | stat-card-warning | stat-card-secondary | stat-card-empty -->
```

**Features**: Gradient background per variant, 3px top accent on hover, lift animation (4px), icon scale on hover.

---

## 5. Status Badges

```html
<span class="status-badge status-live">Live</span>
<span class="status-badge status-draft">Draft</span>
<span class="status-badge status-ended">Ended</span>
<span class="status-badge status-expired">Expired</span>
```

**`status-live`** has a pulsing animated dot and glow. All badges are pill-shaped with gradient backgrounds.

---

## 6. Buttons

```html
<!-- Primary -->
<button class="btn btn-pulse-primary">Primary Action</button>
<button class="btn btn-pulse-primary btn-lift">With lift effect</button>
<button class="btn btn-pulse-primary btn-loading">Loading...</button>
<button class="btn btn-pulse-primary" disabled>Disabled</button>

<!-- Outline -->
<button class="btn btn-pulse-outline">Secondary Action</button>

<!-- Icon button -->
<button class="btn btn-icon btn-primary">
  <i class="fas fa-heart"></i>
</button>
```

**Micro-interactions**: ripple on click, lift on hover, focus-visible outline, spinner loading state.

---

## 7. Form Controls

```html
<!-- Basic input -->
<div class="mb-3">
  <label class="form-label">Email Address</label>
  <input type="email" class="form-control" placeholder="you@example.com">
  <div class="form-text">Helper text displayed below.</div>
</div>

<!-- Validation states -->
<input type="text" class="form-control is-valid">
<div class="valid-feedback">Looks good!</div>

<input type="text" class="form-control is-invalid">
<div class="invalid-feedback">Please provide a valid input.</div>

<!-- Textarea with character counter -->
<div class="mb-3">
  <label class="form-label">Description</label>
  <textarea class="form-control" maxlength="200"></textarea>
  <div class="character-counter">
    <span class="current">0</span> / <span class="max">200</span>
  </div>
</div>
<!-- Add class near-limit or at-limit for color feedback -->

<!-- Toggle switch -->
<div class="form-check form-switch">
  <input class="form-check-input" type="checkbox" id="toggleSwitch">
  <label class="form-check-label" for="toggleSwitch">Enable feature</label>
</div>
```

**Features**: Blue glow ring on focus, hover state before focus, character counters with `near-limit` / `at-limit` color feedback.

---

## 8. Form Sections

```html
<div class="form-section">
  <h5 class="form-section-title">Section Title</h5>
  <div class="mb-3">
    <label class="form-label">Field</label>
    <input type="text" class="form-control">
  </div>
</div>
```

---

## 9. Empty States

```html
<div class="empty-state">
  <div class="empty-state-icon">
    <i class="ics ics-rocket ic-2xl"></i>
  </div>
  <h3 class="empty-state-title">No Items Found</h3>
  <p class="empty-state-text">
    You haven't created any items yet. Get started by creating your first one!
  </p>
  <a href="/create" class="btn btn-pulse-primary btn-lift">
    Create Your First Item
  </a>
</div>
```

**Features**: Dashed border, floating icon animation, pulsing CTA button, centered layout.

---

## 10. Tables

```html
<table class="sessions-table">
  <thead>
    <tr>
      <th>Session</th>
      <th>Status</th>
      <th>Actions</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Sprint Review</td>
      <td><span class="status-badge status-live">Live</span></td>
      <td><a href="#" class="btn btn-sm btn-pulse-outline">Open</a></td>
    </tr>
  </tbody>
</table>
```

**Features**: Left accent border on hover, gradient background on hover, subtle 2px slide animation.

---

## 11. Utility / Micro-Interaction Classes

```html
<div class="btn-lift">Lift on hover (4px translateY)</div>
<button class="btn btn-primary btn-loading">Loading spinner</button>
<div class="fade-in">Content fades in smoothly</div>
<div class="alert alert-danger shake">Error shake animation</div>
<span class="badge bg-success success-checkmark">Saved!</span>
```

### Shadow Utilities

```html
<div class="shadow-xs">   <div class="shadow-sm">   <div class="shadow-md">
<div class="shadow-lg">   <div class="shadow-xl">
<div class="shadow-glow"> <!-- Blue glow effect -->
```

### Glass Effect

```html
<div class="glass">Glassmorphism background</div>
```

### Gradient Text

```html
<h1 class="gradient-text">Blue to Mint Gradient Text</h1>
```

---

## 12. SVG Icon System

Icons are loaded via CSS background images on `<i>` tags. Do NOT use `<img>` tags.

### Base Usage

```html
<!-- Size classes: ic-xs ic-sm ic-md ic-lg ic-xl ic-2xl -->
<!-- Spacing: ic-mr (margin-right) ic-ml (margin-left) -->

<i class="ics ics-rocket ic-lg ic-mr"></i>
<i class="ics ics-check-mark ic-xs"></i>
<i class="ics ics-robot ic-xl"></i>
```

### Size Reference

| Class | Size | Use Case |
|-------|------|---------|
| `ic-xs` | 16px | Badge icons, inline text |
| `ic-sm` | 20px | Toolbar, modal titles |
| `ic-md` | 32px | Section headers, cards |
| `ic-lg` | 48px | Feature cards, template icons |
| `ic-xl` | 64px | Feature highlights |
| `ic-2xl` | 80px | Hero sections, empty states |

### Available Icons (87 total)

```
alarm, bar-chart, bolt, bomb, book-open, bookmark-tabs, books, bust, busts,
calendar, calendar-tear-off, chart, chart-decreasing, chart-increasing, chat,
check-mark, clipboard, collision, confetti, confused, cross-mark, document,
down-arrow, email, exclamation, family, fire, folder, folder-open, globe,
grinning, grinning-big, handshake, heart, hourglass, house, hundred, info,
key, laptop, left-arrow, lightbulb, lightning, location, locked, medal,
medal-sports, memo, minus, mobile, office, ok-hand, party, pen, pencil,
people, pin, plus, pushpin, question, raised-hand, right-arrow, robot, rocket,
sad, save, school, search, settings, smile, smiling, speech-balloon, star,
stopwatch, target, thinking, thought-balloon, thumbs-down, thumbs-up, trash,
trophy, unlocked, up-arrow, warning, waving-hand, wrench, zzz
```

Usage pattern: `<i class="ics ics-{icon-name} ic-{size}"></i>`

---

## 13. Bootstrap 5.3 Preferred Classes

Always try these first before writing custom CSS:

| Need | Bootstrap Class(es) |
|------|-------------------|
| Flexbox row | `d-flex`, `justify-content-between`, `align-items-center` |
| Spacing | `m-*`, `p-*`, `gap-*`, `mb-3`, `mt-4` |
| Text | `text-muted`, `fw-semibold`, `fs-5`, `text-truncate` |
| Color | `text-primary`, `bg-light`, `border-success` |
| Layout | `container`, `row`, `col-md-6` |
| Cards | `card`, `card-body`, `card-title`, `card-text` |
| Buttons | `btn`, `btn-primary`, `btn-outline-secondary`, `btn-sm` |
| Alerts | `alert`, `alert-danger`, `alert-success` |
| Badges | `badge`, `bg-primary`, `rounded-pill` |
| Shadows | `shadow-sm`, `shadow` |
| Border | `rounded`, `rounded-lg`, `border` |
| Display | `d-none`, `d-md-block`, `d-flex` |

---

## 14. Accessibility Checklist

- Focus-visible states on all interactive elements
- Color contrast meets WCAG AA
- Keyboard navigation fully supported
- All icon-only buttons have `aria-label`
- Touch targets minimum 44px on mobile
- Animations respect `prefers-reduced-motion`
- Screen reader text via `visually-hidden` where icons replace text

---

## 15. Do / Don't Reference

| Do | Don't |
|----|-------|
| Use Bootstrap utilities for layout and spacing | Write custom margin/padding classes for standard spacing |
| Use `<i class="ics ics-rocket">` for icons | Use `<img>` tags for icons or unicode emoji characters |
| Use CSS variables for brand colors | Hard-code hex values in styles |
| Use `form-control`, `btn`, `card` Bootstrap classes | Create `.custom-card`, `.my-button` for generic patterns |
| Use `pulse.css` generic classes | Add page-specific styles to `pulse.css` |
| Use `btn-pulse-primary` for primary CTAs | Use `btn-primary` inconsistently across pages |
| Validate inputs with `is-valid` / `is-invalid` | Style validation state with custom classes |
| Add `aria-label` on icon-only buttons | Leave interactive elements without accessible labels |
