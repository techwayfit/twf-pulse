# TechWayFit Pulse - CSS Class Reference

This is a quick reference for the CSS classes used in the Blazor views, matching the HTML templates.

## Layout & Structure

### Page Container
- `.wrap` - Main page wrapper (max-width: 1180px, centered)
- `.grid` - Grid layout container
- `.center` - Centered content (for participant views)

### Cards
- `.card` - Main card container
- `.card.solid` - Solid background variant
- `.phone` - Mobile-sized card (max 520px)

### Grid Patterns
```razor
<!-- Two-column grid -->
<div class="grid">
    <div class="card">...</div>
    <aside class="card solid">...</div>
</div>

<!-- Custom grid columns -->
<div class="grid" style="grid-template-columns: 320px 1fr;">
    <aside class="card">...</aside>
    <section class="viz">...</section>
</div>
```

## Navigation

### Topbar
```razor
<header class="topbar">
    <div class="row">
        <a class="brand" href="/home">
            <div class="logo" aria-hidden="true"></div>
            <div class="name">
                <strong>TechWayFit Pulse</strong>
                <span>Tagline</span>
            </div>
        </a>
        <div class="navbtns">
            <a class="btn ghost">Button</a>
            <a class="btn primary">Primary</a>
        </div>
    </div>
</header>
```

### Buttons
- `.btn` - Base button style
- `.btn.ghost` - Transparent background
- `.btn.primary` - Primary action (gradient)
- `.btn.active` - Active state

## Typography

### Headings
- `.h1` - Large heading (34px)
- `.h2` - Section heading (20px)
- `.p` - Paragraph with muted color
- `.subtle` - Small muted text (13px)

### Text Utilities
- `.right` - Right-aligned text

## Content Sections

### Title Block
```razor
<div class="title">
    <div>
        <h2 class="h2">Heading</h2>
        <div class="subtle">Subtitle</div>
    </div>
    <span class="chip">Status</span>
</div>
```

## Forms

### Form Container
```razor
<div class="form">
    <div>
        <label>Field Label</label>
        <input class="field" />
    </div>
    <div class="row2">
        <div>
            <label>Field 1</label>
            <select class="field">...</select>
        </div>
        <div>
            <label>Field 2</label>
            <select class="field">...</select>
        </div>
    </div>
</div>
```

### Form Elements
- `.form` - Form container
- `.field` - Input/select/textarea styling
- `.row2` - Two-column row
- `label` - Form labels

## Components

### Chips & Badges
```razor
<!-- Chip with status -->
<span class="chip">
    <span class="dot"></span> Live
</span>

<!-- Badge -->
<span class="badge">
    <span class="dot"></span> Status
</span>
```

### Lists
```razor
<div class="list">
    <div class="item">
        <strong>Title</strong>
        <div>Subtitle</div>
    </div>
    <div class="item active">
        <strong>Active Item</strong>
        <div>Selected</div>
    </div>
</div>
```

- `.list` - List container
- `.item` - List item
- `.item.active` - Selected state

### Visualizations
```razor
<div class="viz">
    <div class="vizhead">
        <strong>Chart Title</strong>
        <div class="pulse">
            <span class="b"></span>
            <strong>Updating</strong>
        </div>
    </div>
    <!-- Chart content -->
</div>
```

- `.viz` - Visualization container
- `.vizhead` - Viz header
- `.pulse` - Pulsing indicator
- `.pulse .b` - Animated dot

### Word Cloud
```razor
<div class="cloud">
    <span class="w big">Word1</span>
    <span class="w med">Word2</span>
    <span class="w small">Word3</span>
</div>
```

- `.cloud` - Word cloud container
- `.w.big` - Large word (26px)
- `.w.med` - Medium word (18px)
- `.w.small` - Small word (13px)

### Toolbar
```razor
<div class="toolbar">
    <button class="btn primary">Action</button>
    <button class="btn">Cancel</button>
</div>
```

## Utilities

### Spacing
- `.sp` - Spacer (10px height)
- `.hr` - Horizontal rule divider

### Tabs
```razor
<div class="tabs">
    <button class="tab on">Active</button>
    <button class="tab">Inactive</button>
</div>
```

- `.tabs` - Tab container
- `.tab` - Tab button
- `.tab.on` - Active tab

## Colors (CSS Variables)

```css
--bg: #FFFEEF           /* Page background */
--card: rgba(255,255,255,.78)   /* Card background */
--text: #0F2438         /* Text color */
--muted: rgba(15,36,56,.68)     /* Muted text */

--mint: #2BC48A         /* Success/Active */
--blue: #2D7FF9         /* Primary */
--warn: #FFB020         /* Warning */
--danger: #FF4D6D       /* Error */
```

## Responsive Breakpoints

- Mobile: `@media (max-width: 680px)` - `.row2` becomes single column
- Tablet: `@media (max-width: 980px)` - `.grid` becomes single column

## Common Patterns

### Participant Mobile View
```razor
<main class="wrap">
    <div class="center">
        <div class="card phone">
            <!-- Content -->
        </div>
    </div>
</main>
```

### Facilitator Split View
```razor
<main class="wrap">
    <div class="grid" style="grid-template-columns: 320px 1fr;">
        <aside class="card"><!-- Sidebar --></aside>
        <section class="viz"><!-- Main content --></section>
    </div>
</main>
```

### Dashboard Grid
```razor
<section class="grid">
    <div class="card"><!-- Main area --></div>
    <aside class="card solid"><!-- Filters --></aside>
</section>
```

## Example: Complete Page Structure

```razor
@page "/example"

<PageTitle>Page Title</PageTitle>

<header class="topbar">
    <div class="row">
        <a class="brand" href="/home">
            <div class="logo"></div>
            <div class="name">
                <strong>TechWayFit Pulse</strong>
                <span>Subtitle</span>
            </div>
        </a>
        <div class="navbtns">
            <a class="btn ghost" href="/home">Home</a>
            <a class="btn primary" href="/action">Action</a>
        </div>
    </div>
</header>

<main class="wrap">
    <section class="card">
        <div class="title">
            <h2 class="h2">Section Title</h2>
            <span class="chip"><span class="dot"></span> Live</span>
        </div>
        
        <!-- Content here -->
    </section>
</main>
```
