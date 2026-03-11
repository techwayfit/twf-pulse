# Copy Session Feature - Implementation Summary

## Overview
The Copy Session feature allows facilitators to duplicate existing sessions along with all their activities. This is useful for:
- Running similar workshops repeatedly
- Creating variations of successful sessions
- Testing different configurations

## Requirements

1. ? Create a copy of session and its activities
2. ? Rename session Title to include `- Copy-DDMMYYHHMMSS` stamp
3. ? For activity, do not copy session-specific data like status, metadata
4. ? Session should be in draft state

## Implementation Details

### Backend Implementation

#### 1. Service Layer (`SessionService.cs`)

**Method**: `CopySessionAsync(Guid sessionId, string newCode, DateTimeOffset now, CancellationToken cancellationToken)`

**Logic**:
- Retrieves the original session
- Validates the new session code is unique
- Creates new title with timestamp: `{OriginalTitle} - Copy-{ddMMyyHHmmss}`
- Truncates title if it exceeds 200 character limit
- Creates new session in **Draft** status with:
  - ? All settings copied (TTL, anonymous mode, etc.)
  - ? Join form schema copied
  - ? Group assignment copied
  - ? Session-specific data cleared: `CurrentActivityId`, `SessionStart`, `SessionEnd`
- Copies all activities in order with:
  - ? Same type, title, prompt, config, duration
  - ? Status reset to **Pending** (not Open/Closed)
  - ? Session-specific timestamps cleared: `OpenedAt`, `ClosedAt`

**Code Location**: `src\TechWayFit.Pulse.Application\Services\SessionService.cs`

#### 2. API Controller (`SessionsController.cs`)

**Endpoint**: `POST /api/sessions/{code}/copy`

**Security**:
- Requires authenticated facilitator
- Validates session ownership (only owner can copy)
- Generates unique session code automatically

**Response**: Returns `CreateSessionResponse` with new session ID and code

**Code Location**: `src\TechWayFit.Pulse.Web\Controllers\Api\SessionsController.cs`

#### 3. Interface (`ISessionService.cs`)

Added method signature with XML documentation:

```csharp
/// <summary>
/// Copy a session and its activities to create a new draft session.
/// </summary>
Task<Session> CopySessionAsync(
    Guid sessionId,
    string newCode,
    DateTimeOffset now,
    CancellationToken cancellationToken = default);
```

**Code Location**: `src\TechWayFit.Pulse.Application\Abstractions\Services\ISessionService.cs`

### Frontend Implementation

#### 1. Dashboard Page (`Dashboard.cshtml`)

**Features**:
- Copy button in desktop table view (btn-group with View/Edit/Copy)
- Copy button in mobile card view
- JavaScript handler for copy functionality
- Confirmation dialog before copying
- Loading state during API call
- Success message with new session code
- Auto-redirect to Edit Session page after successful copy

**Button Markup** (Desktop):
```html
<button type="button" 
    class="btn btn-sm btn-outline-info copy-session-btn" 
    data-session-code="@session.Code"
    data-session-title="@session.Title"
  title="Copy Session">
    <i class="fas fa-copy"></i>
</button>
```

**Button Markup** (Mobile):
```html
<button type="button"
    class="btn btn-sm btn-outline-info flex-fill copy-session-btn"
    data-session-code="@session.Code"
    data-session-title="@session.Title"
    title="Copy Session">
  <i class="fas fa-copy"></i> Copy
</button>
```

**JavaScript Handler**:
```javascript
document.addEventListener('click', async function(e) {
    if (e.target.closest('.copy-session-btn')) {
  const btn = e.target.closest('.copy-session-btn');
    const sessionCode = btn.getAttribute('data-session-code');
        const sessionTitle = btn.getAttribute('data-session-title');
   
      // Confirm with user
        if (!confirm(`Copy session "${sessionTitle}"?\n\nThis will create a new draft session with all activities copied.`)) {
   return;
        }
        
        // Show loading state
        const originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
      
    try {
const response = await fetch(`/api/sessions/${sessionCode}/copy`, {
    method: 'POST',
          headers: {
       'Content-Type': 'application/json'
      }
            });
    
            if (response.ok) {
    const result = await response.json();
         const newCode = result.data.code;
   
            // Show success message and redirect
      alert(`Session copied successfully!\n\nNew session code: ${newCode}\n\nRedirecting to edit page...`);
       window.location.href = `/facilitator/edit-session/${newCode}`;
    } else {
     const error = await response.json();
       alert('Error copying session: ' + (error.errors?.[0]?.message || 'Unknown error'));
    btn.disabled = false;
     btn.innerHTML = originalHtml;
            }
        } catch (error) {
            alert('Error copying session: ' + error.message);
            btn.disabled = false;
            btn.innerHTML = originalHtml;
   }
    }
});
```

**Code Location**: `src\TechWayFit.Pulse.Web\Views\Facilitator\Dashboard.cshtml`

#### 2. Groups Page (`Groups.cshtml` + `_GroupCard.cshtml`)

**Features**:
- Copy button added to session items in group cards
- Same JavaScript handler as Dashboard (using event delegation)
- Consistent button styling and behavior
- Same confirmation, loading, and redirect flow

**Session Item Markup** (in `_GroupCard.cshtml`):
```html
<div class="d-flex gap-1" onclick="event.stopPropagation();">
    <a href="/facilitator/live?Code=@session.SessionCode" 
        class="btn btn-sm btn-outline-primary" 
        title="View Session">
        <i class="fas fa-eye"></i>
    </a>
 <a href="/facilitator/edit-session/@session.SessionCode?returnUrl=@Uri.EscapeDataString(Url.Action("Groups", "Facilitator") ?? "/facilitator/groups")" 
    class="btn btn-sm btn-outline-secondary"
        title="Edit Session">
     <i class="fas fa-edit"></i>
    </a>
  <button type="button" 
     class="btn btn-sm btn-outline-info copy-session-btn" 
        data-session-code="@session.SessionCode"
        data-session-title="@session.Title"
        title="Copy Session">
        <i class="fas fa-copy"></i>
    </button>
</div>
```

**JavaScript Handler**: Same as Dashboard, added to `Groups.cshtml` Scripts section

**Code Locations**: 
- `src\TechWayFit.Pulse.Web\Views\Facilitator\_GroupCard.cshtml`
- `src\TechWayFit.Pulse.Web\Views\Facilitator\Groups.cshtml`

## User Experience Flow

### On Dashboard or Groups Page:

1. **User clicks Copy button** on any session
2. **Confirmation dialog** appears:
   ```
   Copy session "{SessionTitle}"?
   
   This will create a new draft session with all activities copied.
   ```
3. **User confirms** ? Loading spinner appears on button
4. **API call** to `POST /api/sessions/{code}/copy`
5. **Success response** ? Alert message:
   ```
   Session copied successfully!
   
   New session code: XXX-XXX-XXX
   
   Redirecting to edit page...
   ```
6. **Auto-redirect** to Edit Session page for the new copy

### What Gets Copied:

? **Copied**:
- Session title (with timestamp suffix)
- Goal, Context, Group assignment
- Settings (TTL, anonymous mode, strict mode)
- Join form schema (all fields)
- All activities (type, title, prompt, config, duration, order)

? **NOT Copied** (Reset/Cleared):
- Session status ? **Draft**
- Current activity ? `null`
- Session start/end times ? `null`
- Activity statuses ? **Pending**
- Activity opened/closed timestamps ? `null`
- Responses and participants ? Not copied (new session starts fresh)

## Title Naming Convention

**Format**: `{Original Title} - Copy-{ddMMyyHHmmss}`

**Examples**:
- Original: `"Sprint Retrospective"`
- Copied on Feb 15, 2025 at 14:30:45
- New Title: `"Sprint Retrospective - Copy-150225143045"`

**Length Handling**:
- Max title length: 200 characters
- If combined title exceeds 200 chars, original title is truncated
- Timestamp suffix is always preserved

## Testing Checklist

- [x] Copy button appears on Dashboard (desktop table)
- [x] Copy button appears on Dashboard (mobile cards)
- [x] Copy button appears on Groups page (session items)
- [x] Confirmation dialog shows before copying
- [x] Loading state during API call
- [x] Success message shows new code
- [x] Auto-redirect to Edit Session page
- [x] New session has timestamped title
- [x] New session is in Draft status
- [x] All activities are copied
- [x] Activity statuses are reset to Pending
- [x] Original session remains unchanged
- [x] Only session owner can copy
- [x] Unique session code generated
- [x] Group assignment preserved

## Error Handling

**Client-side**:
- User cancels confirmation ? No API call made
- API call fails ? Error alert shown, button restored
- Network error ? Error alert shown, button restored

**Server-side**:
- Session not found ? 404 Not Found
- Unauthorized (not owner) ? 401 Unauthorized
- Code generation fails ? 400 Bad Request
- Database error ? 500 Internal Server Error

## Files Modified

### Backend:
1. `src\TechWayFit.Pulse.Application\Abstractions\Services\ISessionService.cs` - Added method signature
2. `src\TechWayFit.Pulse.Application\Services\SessionService.cs` - Implemented CopySessionAsync
3. `src\TechWayFit.Pulse.Web\Controllers\Api\SessionsController.cs` - Added POST /copy endpoint

### Frontend:
1. `src\TechWayFit.Pulse.Web\Views\Facilitator\Dashboard.cshtml` - Added Copy buttons and JS handler
2. `src\TechWayFit.Pulse.Web\Views\Facilitator\_GroupCard.cshtml` - Added Copy button to session items
3. `src\TechWayFit.Pulse.Web\Views\Facilitator\Groups.cshtml` - Added Copy JS handler

## Future Enhancements

Potential improvements for consideration:
- Bulk copy multiple sessions
- Copy to different group during copy operation
- Option to exclude certain activities from copy
- Copy session settings customization dialog
- Template creation from copied session

## Related Documentation

- [Session Management](./02-functional-details.md#2-core-concepts)
- [Activity Types](./03-activity-types.md)
- [Session Templates](./02-functional-details.md#8-session-templates)
