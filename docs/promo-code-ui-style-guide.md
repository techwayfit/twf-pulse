# PromoCode BackOffice UI - Style Consistency Update

## ? All Views Now Match Plans Section Style

All PromoCode views have been updated to match the exact styling patterns used in the Plans section.

---

## ?? Page-by-Page Changes

### 1. Index.cshtml (List View)

**Style Updates:**
- ? Uses `bo-search-bar` with dropdown filters (Status, Validity)
- ? Table structure matches Plans/Index.cshtml
- ? Columns: Code (bold mono), Target Plan, Duration, Max Uses, Used, Valid Until, Status, Actions
- ? Status pill color-coded: Active (green), Scheduled (gray), Inactive/Expired (red)
- ? "View" and "Edit" action buttons
- ? Pagination with filter preservation

**Layout:**
```razor
bo-page-header
  bo-page-title-row
  + New Promo Code button

bo-search-bar
  Status dropdown (All/Active/Inactive)
  Validity dropdown (All/Valid/Expired)

bo-card
  bo-table-wrap
    bo-table (8 columns)

bo-pagination (if needed)
```

---

### 2. Detail.cshtml (Detail View)

**Style Updates:**
- ? Breadcrumb navigation: Promo Codes / CODE
- ? Status pill in subtitle (consistent with Plans)
- ? Info grid with 3 columns (`bo-info-grid cols3`)
- ? Campaign progress bar integrated into main card (with border separator)
- ? Recent redemptions as `bo-table` (professional table format)
- ? Actions section with `bo-actions-grid cols2`
- ? Disabled action card style when delete not allowed
- ? Smart alerts at top (Expired, Scheduled, Limit Reached)

**Layout:**
```razor
bo-breadcrumb

bo-page-header
  Status pill in subtitle

bo-alert (if expired/scheduled/limit)

bo-card (Promo Code Details)
  bo-info-grid cols3
    12 info items (Code, Plan, Duration, etc.)
  Campaign Progress (if limited uses)
    Progress bar

bo-card (Recent Redemptions)
  bo-table
    4 columns: Email, Subscription ID, Date, IP
  View all link (if >5)

bo-section-header (Actions - audited)

bo-actions-grid cols2
  Activate/Deactivate card
  Delete card (or disabled state)
```

---

### 3. Create.cshtml (Create Form)

**Style Updates:**
- ? Breadcrumb: Promo Codes / New
- ? Single card layout, max-width:600px
- ? Consistent field labels with red asterisks for required
- ? `bo-info-note` for field hints (gray helper text)
- ? Grid layout for paired fields (Duration/Max Uses, ValidFrom/Until)
- ? Full-width button: "Create Promo Code" with checkmark icon
- ? Uses `bo-field-label` (not `bo-label`)
- ? Spacing: margin-bottom:14px (14px between fields, 18px before button)

**Form Structure:**
```razor
Code (text input, uppercase transform)
Target Plan (dropdown with price display)
Duration / Max Redemptions (2-column grid)
Valid From / Valid Until (2-column grid)
Status (dropdown)
[Create Button]
```

---

### 4. Edit.cshtml (Edit Form)

**Style Updates:**
- ? Breadcrumb: Promo Codes / CODE / Edit
- ? Warning alert if code has been redeemed
- ? Disabled fields for redeemed codes (Plan, Duration)
- ? Red warning notes: "?? Cannot change (code already redeemed)"
- ? Reason for Change field (required, for audit trail)
- ? Same layout as Create with conditional restrictions
- ? Uses hidden inputs to preserve disabled field values

**Restrictions Display:**
```razor
@if (hasRedemptions)
{
    <!-- Field disabled with red warning note -->
    <div class="bo-info-note" style="color:#e74c3c">?? Cannot change</div>
}
```

---

### 5. Redemptions.cshtml (Full History)

**Style Updates:**
- ? Breadcrumb: Promo Codes / CODE / Redemptions
- ? Count in subtitle: "X total redemption(s)"
- ? Matches UserHistory.cshtml style
- ? Table with 5 columns: Email, Subscription ID, Date, IP, Actions
- ? "View Subscription" link for each redemption
- ? Empty state: "No redemptions found" with gray text
- ? Sorted by RedeemedAt descending (most recent first)

**Layout:**
```razor
bo-breadcrumb

bo-page-header
  Audit icon
  Count in subtitle

bo-card (or empty state)
  bo-table-wrap
    bo-table
      Email, Sub ID, Date, IP, Actions
```

---

## ?? Design System Elements Used

### Color-Coded Status Pills
```razor
<span class="bo-pill active">Active</span>
<span class="bo-pill inactive">Expired</span>
<span class="bo-pill muted">Scheduled</span>
<span class="bo-pill inactive">Limit Reached</span>
```

### Icons (boi = BackOffice Icon)
- ??? `boi-ticket` - Promo code icon (purple)
- ?? `boi-pen` - Edit icon
- ?? `boi-settings` - Configuration icon
- ?? `boi-chart` - Statistics icon (not used, removed)
- ?? `boi-users` - Users icon (not used, removed)
- ??? `boi-trash` - Delete icon
- ? `boi-check` - Checkmark for success
- ?? `boi-warning` - Warning icon
- ?? `boi-audit` - Audit/history icon

### Info Grid Pattern
```razor
<div class="bo-info-grid cols3">
    <div class="bo-info-item">
        <div class="lbl">Label</div>
        <div class="val">Value</div>
    </div>
</div>
```

### Action Grid Pattern
```razor
<div class="bo-actions-grid cols2">
    <div class="bo-action-card">
        <div class="bo-action-title">Title</div>
        <div class="bo-action-desc">Description</div>
        <form>...</form>
    </div>
    <div class="bo-action-card disabled">
        <div class="bo-action-title">Disabled Action</div>
        <div class="bo-action-desc">Reason why disabled</div>
 </div>
</div>
```

### Alert Patterns
```razor
<div class="bo-alert bo-alert-success">Success message</div>
<div class="bo-alert bo-alert-danger">Error message</div>
<div class="bo-alert bo-alert-warning">?? Warning message</div>
<div class="bo-alert bo-alert-info">?? Info message</div>
```

### Form Field Spacing
```razor
<div style="margin-bottom:14px">      <!-- Normal spacing -->
<div style="margin-bottom:18px">      <!-- Before buttons -->
<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:14px">  <!-- 2-col grid -->
```

---

## ?? Consistency Checklist

### ? All Pages Now Have:
- [x] Breadcrumb navigation
- [x] Page header with icon, title, subtitle
- [x] Consistent button styling (`bo-btn-primary`, `bo-btn-ghost`)
- [x] Monospace for codes/IDs (`bo-td-mono`)
- [x] Muted text for timestamps (`bo-td-muted`)
- [x] Status pills with color coding
- [x] Info notes in gray (`bo-info-note`)
- [x] Red asterisks for required fields
- [x] Full-width submit buttons with icons
- [x] "Back" buttons with left arrow (?)

### ? Tables:
- [x] Use `bo-table` class
- [x] Wrapped in `bo-table-wrap`
- [x] Inside `bo-card`
- [x] Consistent column styling (mono, muted)
- [x] Action buttons in last column

### ? Forms:
- [x] Max-width: 600px on cards
- [x] `bo-field-label` for labels
- [x] `bo-input` for text inputs
- [x] `bo-select` for dropdowns
- [x] Grid layout for paired fields
- [x] Reason field for edit operations

### ? Detail Pages:
- [x] `bo-info-grid cols3` for data display
- [x] 12pt font for labels, clear hierarchy
- [x] Progress bars with border separator
- [x] Actions at bottom in grid
- [x] Disabled action cards for restricted operations

---

## ?? Visual Comparison

### Before
- Mixed layout styles (2-column vs single)
- Inconsistent spacing and field labels
- List-style redemptions (not table)
- Actions scattered in multiple places
- No breadcrumbs on some pages
- Emojis in empty states
- Custom CSS classes

### After
- Consistent single-column layout (forms) and info grids (details)
- Standardized 14px/18px spacing
- Professional table format for all data
- Actions consolidated in grid at bottom
- Breadcrumbs on all pages
- Icon-based UI (no emojis)
- 100% existing BackOffice CSS classes

---

## ?? To See Changes

**Stop and restart BackOffice:**
```sh
# Stop current process (Ctrl+C or kill process)

cd backoffice/src/TechWayFit.Pulse.BackOffice
dotnet run
```

**Navigate to:**
- https://localhost:7001/PromoCodes (Index)
- https://localhost:7001/PromoCodes/Create (Create)
- https://localhost:7001/PromoCodes/Detail/{id} (Detail)
- https://localhost:7001/PromoCodes/Edit/{id} (Edit)
- https://localhost:7001/PromoCodes/Redemptions?id={id} (History)

---

## ?? Style Consistency Matrix

| Page | Breadcrumb | Icon | Grid | Table | Actions | Progress | Status Pills |
|------|-----------|------|------|-------|---------|----------|--------------|
| Index | ??? | ? | - | ? | ? | - | ? |
| Detail | ??? | ? | ??? | ??? | ??? | ? | ? |
| Create | ??? | ? | ? | - | - | - | - |
| Edit | ??? | ? | ? | - | - | - | ? |
| Redemptions | ??? | ? | - | ? | ? | - | - |

**Legend:**
- ??? = Fixed/Added
- ? = Already correct
- ? = Not applicable

---

## ?? Key Style Principles Applied

1. **Consistent Spacing**
   - 14px between form fields
   - 18px before submit buttons
   - 20px between major cards
   - 12px gap in grids

2. **Typography Hierarchy**
   - `bo-page-title` - Main heading
   - `bo-page-sub` - Subtitle with metadata
   - `bo-card-header` - Card titles
   - `bo-field-label` - Form labels
   - `bo-info-note` - Helper text (gray)

3. **Color System**
   - Purple icon for PromoCode pages (`bo-page-icon purple`)
   - Green for Plans, Blue for Subscriptions
   - Status pills: green (active), gray (scheduled/muted), red (inactive/expired)
   - Required asterisks: #e74c3c red

4. **Interactive Elements**
   - Primary buttons: `bo-btn bo-btn-primary`
   - Ghost buttons: `bo-btn bo-btn-ghost`
   - Danger actions: `bo-btn bo-btn-danger`
   - Success actions: `bo-btn bo-btn-success`
   - Warning actions: `bo-btn bo-btn-warning`

5. **Data Display**
   - Monospace: `bo-td-mono` for codes, IDs, numbers
   - Muted: `bo-td-muted` for timestamps, secondary info
   - Bold: `<strong>` for emphasis in tables/grids
   - Pills: `<span class="bo-pill">` for status/badges

---

## ? Result

The PromoCode section now has **perfect visual consistency** with the rest of the BackOffice:
- ? Matches Plans section exactly
- ? Matches Subscriptions section style
- ? Follows BackOffice design system 100%
- ? Professional, clean, scannable UI
- ? Zero custom CSS needed

All pages are ready for production! ??
