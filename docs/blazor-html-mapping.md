# HTML to Blazor Page Mapping

This document shows the mapping between the HTML templates in `docs/html` and the new Blazor pages.

## Page Mapping

| HTML Template | Blazor Page | Route | Description |
|--------------|-------------|-------|-------------|
| `index.html` | `Pages/Home.razor` | `/home` | Homepage with templates and getting started |
| `create.html` | `Pages/Facilitator/CreateWorkshop.razor` | `/facilitator/create-workshop` | Session creation wizard |
| `live.html` | `Pages/Facilitator/Live.razor` | `/facilitator/live` | Live facilitator console with agenda |
| `dashboards.html` | `Pages/Facilitator/Dashboards.razor` | `/facilitator/dashboards` | Real-time dashboards and analytics |
| `participant-join.html` | `Pages/Participant/JoinSession.razor` | `/participant/join-session` | Participant join form |
| `participant-activity.html` | `Pages/Participant/ActivityView.razor` | `/participant/activity-view` | Participant activity view |
| `participant-done.html` | `Pages/Participant/DoneView.razor` | `/participant/done-view` | Thank you page after completion |

## CSS Files

- **Template CSS**: `docs/html/assets/styles.css` 
- **Blazor CSS**: `src/TechWayFit.Pulse.Web/wwwroot/css/pulse-workshop.css` (copied from template)
- **Layout CSS Reference**: Updated in `Pages/Shared/_Layout.cshtml`

## Key Features Implemented

### 1. Consistent Header Navigation
All pages now have the identical topbar navigation structure from the HTML templates:
- Brand logo and name
- Navigation buttons (Home, Create, Live Room, Dashboards, Participant)
- Primary action button (context-specific)

### 2. Matching Layout Structure
- Grid layouts for multi-column views
- Card-based design system
- Centered phone layout for participant views
- Sidebar + main content for facilitator console

### 3. Identical CSS Classes
All CSS classes from the HTML templates are preserved:
- `.topbar`, `.brand`, `.logo`, `.name`
- `.card`, `.solid`, `.title`, `.chip`, `.badge`
- `.form`, `.field`, `.row2`, `.toolbar`
- `.list`, `.item`, `.active`
- `.viz`, `.vizhead`, `.cloud`
- `.center`, `.phone`

### 4. Visual Elements
- Live pulse indicator (`.pulse` with animation)
- Dot indicators for status (`.dot`)
- Word cloud visualization
- SVG quadrant charts
- Filters and chips

## Navigation Flow

```
/ (Index.razor) → Redirects to /home
↓
/home (Home.razor)
├─→ /facilitator/create-workshop (CreateWorkshop.razor)
│   └─→ /facilitator/live (Live.razor)
│       └─→ /facilitator/dashboards (Dashboards.razor)
└─→ /participant/join-session (JoinSession.razor)
    └─→ /participant/activity-view (ActivityView.razor)
        └─→ /participant/done-view (DoneView.razor)
```

## Existing Pages

The following existing pages are preserved for backward compatibility and functional features:
- `Pages/Facilitator/Create.razor` - Original create page with full functionality
- `Pages/Facilitator/Console.razor` - Original console with SignalR integration
- `Pages/Participant/Join.razor` - Original join with API integration
- `Pages/Participant/Activity.razor` - Original activity with real-time updates

## Usage

To use the new template-based pages:
1. Navigate to `/home` to see the new homepage
2. Use the navigation buttons to explore different sections
3. The new pages are pure UI templates matching the HTML designs
4. The original pages (without `-workshop`, `-session`, `-view` suffixes) contain the full backend integration

## Next Steps

1. **Merge Functionality**: Integrate the backend logic from original pages into the new template pages
2. **SignalR Integration**: Add real-time updates to the new template pages
3. **API Calls**: Connect the new forms to the backend APIs
4. **State Management**: Implement proper state management across pages
5. **Validation**: Add form validation and error handling
6. **Responsive Design**: Test and optimize for mobile devices
