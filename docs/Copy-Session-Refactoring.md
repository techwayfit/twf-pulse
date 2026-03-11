# Copy Session Feature - Refactored Implementation

## Overview
The Copy Session feature has been refactored into reusable components that can be shared across multiple pages.

## Files Created

### 1. Shared Partial View
**File**: `src/TechWayFit.Pulse.Web/Views/Shared/_CopySessionModals.cshtml`

Contains three Bootstrap modals:
- **Copy Confirmation Modal** (`#copySessionModal`) - Confirms the copy action
- **Copy Success Modal** (`#copySuccessModal`) - Shows success with new session code
- **Copy Error Modal** (`#copyErrorModal`) - Displays error messages

### 2. Shared JavaScript Module
**File**: `src/TechWayFit.Pulse.Web/wwwroot/js/copy-session.js`

Provides:
- Auto-initialization on DOM ready
- Modal-based UI (no browser alerts)
- Proper error handling
- Comprehensive logging
- Global API: `window.CopySession`

## Usage

### In Any View

1. **Include the partial view** (before `@section Scripts`):
```razor
@await Html.PartialAsync("_CopySessionModals")
```

2. **Include the JavaScript** (in `@section Scripts`):
```razor
<script src="~/js/copy-session.js"></script>
```

3. **Add copy buttons** with required data attributes:
```html
<button type="button" 
        class="copy-session-btn" 
     data-session-code="ABC123"
      data-session-title="My Session">
    <i class="fas fa-copy"></i> Copy
</button>
```

## Required Data Attributes

| Attribute | Required | Description |
|-----------|----------|-------------|
| `data-session-code` | ? | The session code to copy |
| `data-session-title` | ? | Session title for display |
| `class="copy-session-btn"` | ? | Required class for auto-detection |

## API Endpoint

**POST** `/api/sessions/{code}/copy`

- **Auth**: Requires authenticated facilitator (session owner)
- **Returns**: `{ data: { code: "NEWCODE", ... } }`

## ?? User Experience Flow

1. **Click Copy Button** ? Shows confirmation modal
2. **Click "Copy Session"** ? Button shows spinner, makes API call
3. **On Success** ? Shows success modal with new session code
   - **"Stay Here" button** - Closes modal and **reloads the page** to show the newly copied session
   - **"Edit New Session" button** - Navigates to edit page for the new session
4. **On Error** ? Shows error modal with details

## Features

? **Auto-initialization** - No manual setup required  
? **Bootstrap modals** - Professional UI  
? **Loading states** - Visual feedback during API calls  
? **Error handling** - Comprehensive error messages  
? **Console logging** - Debugging support  
? **Responsive** - Works on mobile and desktop  

## Current Implementation

### Pages Using Copy Session

1. **Dashboard** (`Views/Facilitator/Dashboard.cshtml`)
   - Desktop table view
   - Mobile card view
   - Success action: "Stay on Dashboard" or "Edit New Session"

2. **Groups** (`Views/Facilitator/Groups.cshtml`)
   - Group card sessions list
   - Success action: "Stay Here" or "Edit New Session"

## Customization

### Custom Success Behavior

```javascript
// Customize the "Edit New Session" button behavior
document.getElementById('goToEditBtn').onclick = function() {
    // Custom navigation or action
    window.location.href = `/custom/path/${newCode}`;
};
```

### Manual Initialization

```javascript
// If you need to re-initialize after dynamic content
window.CopySession.initialize();
```

### Show Error Manually

```javascript
window.CopySession.showError('Custom error message');
```

### Show Success Manually

```javascript
window.CopySession.showSuccess('NEWCODE123');
```

## Migration Guide

### Old Implementation (Inline)

```razor
<!-- OLD: Inline modals -->
<div class="modal" id="copySessionModal">...</div>
<div class="modal" id="copySuccessModal">...</div>
<div class="modal" id="copyErrorModal">...</div>

@section Scripts {
  <script>
        // Inline JavaScript for copy functionality
        document.addEventListener('click', async function(e) {
      if (e.target.closest('.copy-session-btn')) {
          // ... hundreds of lines ...
    }
        });
    </script>
}
```

### New Implementation (Refactored)

```razor
<!-- NEW: Shared partial -->
@await Html.PartialAsync("_CopySessionModals")

@section Scripts {
    <!-- NEW: External JavaScript -->
    <script src="~/js/copy-session.js"></script>
    
    <!-- Your page-specific JavaScript -->
    <script>
     // Other page functionality
    </script>
}
```

## Benefits

1. **DRY Principle** - No code duplication
2. **Maintainability** - Single source of truth
3. **Consistency** - Same UI/UX across pages
4. **Testability** - Easier to test standalone module
5. **Performance** - Cached JavaScript file
6. **Modularity** - Can be reused in any view

## Browser Support

- ? Chrome/Edge 90+
- ? Firefox 88+
- ? Safari 14+
- ? Mobile browsers (iOS 14+, Android 11+)

## Dependencies

- **Bootstrap 5.3** - For modals
- **Fetch API** - For API calls (native browser support)
- **Modern JavaScript** - ES6+ (async/await, arrow functions)

## Error Handling

The module handles:
- ? Network errors
- ? API errors (4xx, 5xx)
- ? Invalid responses
- ? Missing data attributes
- ? Bootstrap not loaded

All errors are logged to console and displayed in user-friendly error modal.

## Future Enhancements

Potential improvements:
- [ ] Toast notifications instead of modals
- [ ] Copy multiple sessions at once
- [ ] Copy session to different group
- [ ] Customize session title during copy
- [ ] Progress indicator for large sessions

## Related Files

- `src/TechWayFit.Pulse.Web/Controllers/Api/SessionsController.cs` - API endpoint
- `src/TechWayFit.Pulse.Application/Services/SessionService.cs` - Business logic
- `src/TechWayFit.Pulse.Web/Views/Facilitator/_GroupCard.cshtml` - Copy button HTML

## Troubleshooting

### Copy button not working

1. **Check console** for errors
2. **Verify data attributes** are present
3. **Ensure script is loaded** (check Network tab)
4. **Check Bootstrap** is loaded

### Modal not showing

1. **Verify partial view** is included
2. **Check Bootstrap** modal CSS/JS is loaded
3. **Look for JavaScript errors** in console

### API returns 401 Unauthorized

1. **Verify user** is authenticated
2. **Check session ownership** (user must own the session)
3. **Verify credentials** are included in fetch request

## Support

For issues or questions:
1. Check console logs for detailed error messages
2. Review this documentation
3. Check API endpoint logs
4. Contact development team

---

**Last Updated**: 2025-01-08  
**Version**: 1.0  
**Author**: AI Assistant
