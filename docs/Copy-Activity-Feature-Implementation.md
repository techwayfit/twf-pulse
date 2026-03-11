# Copy Activity Feature Implementation

## Summary

Implemented the ability to **copy activities** and ensured **edit buttons** are visible on both the Live facilitator page and the Add Activities page.

## Features Implemented

### 1. Backend - Copy Activity Service

**Files Modified:**
- `src/TechWayFit.Pulse.Application/Abstractions/Services/IActivityService.cs`
- `src/TechWayFit.Pulse.Application/Services/ActivityService.cs`
- `src/TechWayFit.Pulse.Web/Controllers/Api/SessionsController.cs`

**Functionality:**
- Added `CopyActivityAsync` method that:
  - Creates a new activity with " (Copy)" appended to the title
  - Truncates title if necessary to stay within 200 character limit
  - Creates copy as `Pending` status (even if source is Open/Closed)
  - Appends to the end of the activity list
  - Preserves all other properties (Type, Prompt, Config, DurationMinutes)

**API Endpoint:**
```
POST /api/sessions/{code}/activities/{activityId}/copy
```

**Response:**
```json
{
  "data": {
    "activityId": "guid",
    "order": 2,
    "type": "Poll",
    "title": "Original Activity (Copy)",
    "prompt": "...",
    "config": "{}",
    "status": "Pending",
    "openedAt": null,
    "closedAt": null,
    "durationMinutes": 5
  }
}
```

### 2. Live Page - Activities List Sidebar

**Files Modified:**
- `src/TechWayFit.Pulse.Web/Components/Facilitator/LiveActivitiesList.razor`
- `src/TechWayFit.Pulse.Web/Pages/Facilitator/Live.razor`
- `src/TechWayFit.Pulse.Web/Pages/Facilitator/Live.razor.cs`

**Changes:**
1. **Added Action Buttons Column:**
   - Edit button (visible only for `Pending` activities)
   - Copy button (visible for all activities)
   - Buttons use minimal styling to fit in sidebar
   
2. **Event Handlers:**
   - `OnEditActivity` - Opens EditActivityModal
   - `OnCopyActivity` - Calls CopyActivity method

3. **CopyActivity Method:**
   - Validates facilitator authentication
   - Calls the copy API endpoint
   - Broadcasts SignalR event for real-time updates
   - Reloads the session to show the new activity
   - Shows success toast notification

**UI Changes:**
```razor
<!-- Edit button (Pending only) -->
<button type="button" 
   class="btn btn-xs btn-outline-secondary" 
        title="Edit Activity"
        @onclick="@(() => OnEditActivity.InvokeAsync(activity))">
  <i class="fas fa-pencil"></i>
</button>

<!-- Copy button (All activities) -->
<button type="button" 
        class="btn btn-xs btn-outline-primary" 
  title="Copy Activity"
        @onclick="@(() => OnCopyActivity.InvokeAsync(activity.ActivityId))">
    <i class="fas fa-copy"></i>
</button>
```

### 3. Add Activities Page

**Files Modified:**
- `src/TechWayFit.Pulse.Web/wwwroot/js/activity-types.js`
- `src/TechWayFit.Pulse.Web/wwwroot/js/add-activities.js`

**Changes:**

1. **Activity Cards (`activity-types.js`):**
   - Updated `renderCard()` method to include Edit and Copy buttons
   - Edit button - Opens the activity modal in edit mode
   - Copy button - Calls `activityManager.copyActivity(index)`

2. **AddActivitiesManager (`add-activities.js`):**
 - Added `copyActivity(index)` method that:
     - Gets the activity ID from the array
  - Calls the copy API endpoint
     - Adds the copied activity to the local array
     - Updates the UI
     - Shows success/error notifications

**Button Layout:**
```html
<div class="d-flex gap-2">
 <button class="btn btn-sm btn-primary" 
     onclick="activityManager.editActivity(${index})">
      <i class="ics ics-pencil ic-xs ic-mr"></i>Edit
    </button>
    <button class="btn btn-sm btn-outline-primary" 
         onclick="activityManager.copyActivity(${index})" 
   title="Copy Activity">
        <i class="ics ics-copy ic-xs ic-mr"></i>Copy
    </button>
    <button class="btn btn-sm btn-outline-danger" 
   onclick="activityManager.removeActivity(${index})">
        <i class="ics ics-trash ic-xs ic-mr"></i>Remove
    </button>
</div>
```

### 4. Tests

**Files Created:**
- `tests/TechWayFit.Pulse.Tests/Application/Services/ActivityServiceTests.cs`

**Test Coverage:**
- ? `CopyActivityAsync_Should_Create_Copy_Successfully` - Verifies basic copy functionality
- ? `CopyActivityAsync_Should_Truncate_Title_If_Too_Long` - Ensures titles stay within limits
- ? `CopyActivityAsync_Should_Throw_When_Activity_Not_Found` - Error handling
- ? `CopyActivityAsync_Should_Throw_When_Activity_Belongs_To_Different_Session` - Security check
- ? `CopyActivityAsync_Should_Preserve_All_Properties_Except_Status` - Verifies all properties are copied

## User Workflows

### Copy Activity from Live Page

1. Facilitator is on `/facilitator/live` page
2. In the activities sidebar, each activity shows Edit (if Pending) and Copy buttons
3. Click **Copy** button on any activity
4. System creates a new copy with " (Copy)" appended to title
5. New activity appears at bottom of the list as `Pending`
6. SignalR broadcasts update to all connected clients
7. Success toast notification appears

### Copy Activity from Add Activities Page

1. Facilitator is on `/facilitator/add-activities` page
2. Each activity card shows Edit, Copy, and Remove buttons
3. Click **Copy** button on any activity
4. System creates a new copy via API
5. New activity card appears at bottom of the list
6. Success notification modal appears

### Edit Activity (Existing Feature - Now More Visible)

1. Edit button visible on:
   - Live page sidebar (Pending activities only)
   - Add Activities page (all activities)
2. Click **Edit** button
3. Activity modal opens pre-filled with current values
4. Make changes and save
5. Activity updates in real-time

## Technical Implementation Details

### Copy Logic

```csharp
public async Task<Activity> CopyActivityAsync(
    Guid sessionId,
    Guid activityId,
    CancellationToken cancellationToken = default)
{
    // 1. Validate inputs
    // 2. Get source activity
    // 3. Get all activities to determine new order
    // 4. Create title with " (Copy)" suffix
// 5. Truncate if needed (max 200 chars)
    // 6. Create new activity with:
    //    - New GUID
    //    - Status = Pending
    //    - Order = last + 1
    //    - All other properties preserved
    // 7. Save to database
    // 8. Return new activity
}
```

### Title Truncation Logic

```csharp
var copiedTitle = sourceActivity.Title;
const string copySuffix = " (Copy)";

if (copiedTitle.Length + copySuffix.Length > TitleMaxLength)
{
    copiedTitle = copiedTitle.Substring(0, TitleMaxLength - copySuffix.Length);
}
copiedTitle += copySuffix;
```

### SignalR Integration

The copy operation broadcasts a `ActivityStateChanged` event via SignalR Hub to notify all connected clients about the new activity.

## Design Decisions

### 1. Always Create as Pending
Even if copying an Open or Closed activity, the copy is always created as Pending. This allows facilitators to review and edit before opening.

### 2. Append to End
Copied activities are added to the end of the activity list, maintaining a clear sequence.

### 3. Title Suffix
" (Copy)" is appended to make it clear which activities are copies. Multiple copies get the same suffix (e.g., "Activity (Copy)", "Activity (Copy) (Copy)").

### 4. Preserve All Configuration
The entire `config` JSON is preserved, ensuring complex activities (Polls, Quadrants, etc.) are fully copied with all their settings.

### 5. Button Visibility
- **Edit**: Only visible for Pending activities (can't edit live/closed activities)
- **Copy**: Visible for all activities (can copy any activity regardless of status)
- **Remove**: Only visible for Pending activities

## Styling

### Live Page Buttons
- Small buttons (`btn-xs`) to fit in compact sidebar
- Icon-only for Edit and Copy (no text to save space)
- Outline style for Copy to differentiate from Edit
- `@onclick:stopPropagation="true"` to prevent selecting the activity when clicking buttons

### Add Activities Page Buttons
- Standard small buttons (`btn-sm`)
- Text + icon for better clarity
- Edit: `btn-primary`
- Copy: `btn-outline-primary`
- Remove: `btn-outline-danger`

## Future Enhancements

### Potential Improvements:
1. **Bulk Copy** - Copy multiple activities at once
2. **Copy to Another Session** - Copy activities across sessions
3. **Copy with Responses** - Option to copy activity including responses (for templates)
4. **Smart Naming** - More sophisticated copy naming (e.g., "Activity 2", "Activity 3" instead of multiple "(Copy)")
5. **Copy History** - Track which activities were copied from which source
6. **Undo Copy** - Quick undo action after copying

## Migration Notes

No database migrations required - the copy functionality uses existing Activity entity structure.

## Testing Checklist

- [x] Unit tests for `CopyActivityAsync` method
- [x] API endpoint returns correct response
- [x] Copy button appears on Live page sidebar
- [x] Copy button appears on Add Activities page cards
- [x] Edit button visible for Pending activities
- [x] Clicking Copy creates new activity with " (Copy)" suffix
- [x] Copied activity appears at bottom of list
- [x] Copied activity status is always Pending
- [x] Long titles are truncated properly
- [x] SignalR broadcasts copy event
- [x] Success notifications appear
- [x] Error handling works correctly
- [x] Multiple copies can be created
- [x] Copying preserves all activity configuration

## Known Limitations

1. **Title Length**: Titles longer than 193 characters will be truncated when copying (to accommodate " (Copy)" suffix)
2. **No Duplicate Detection**: System doesn't prevent creating multiple copies of the same activity
3. **No Copy Tracking**: There's no link between original and copied activities
4. **Session-Only Copying**: Cannot copy activities across different sessions (could be future enhancement)

## Related Files

### Backend
- `src/TechWayFit.Pulse.Application/Abstractions/Services/IActivityService.cs`
- `src/TechWayFit.Pulse.Application/Services/ActivityService.cs`
- `src/TechWayFit.Pulse.Web/Controllers/Api/SessionsController.cs`

### Frontend - Live Page
- `src/TechWayFit.Pulse.Web/Components/Facilitator/LiveActivitiesList.razor`
- `src/TechWayFit.Pulse.Web/Pages/Facilitator/Live.razor`
- `src/TechWayFit.Pulse.Web/Pages/Facilitator/Live.razor.cs`

### Frontend - Add Activities Page
- `src/TechWayFit.Pulse.Web/wwwroot/js/activity-types.js`
- `src/TechWayFit.Pulse.Web/wwwroot/js/add-activities.js`

### Tests
- `tests/TechWayFit.Pulse.Tests/Application/Services/ActivityServiceTests.cs`

## Screenshots

### Live Page - Activities Sidebar
```
???????????????????????????????????????
? Activities             [5] ?
???????????????????????????????????????
? #1 ?? Poll            ?
?    Change Sentiment ?
?    Pending     ?
?    [?? Edit] [?? Copy]              ?
???????????????????????????????????????
? #2 ?? Word Cloud        ?
?  Concerns & Hopes       ?
?  Active   ?
?    [?? Copy]     ?
???????????????????????????????????????
```

### Add Activities Page - Activity Cards
```
???????????????????????????????????????????????
? #1 ?? Poll  ?
? Change Sentiment        ?
? 5 options       ?
?  ?
? [?? Edit] [?? Copy] [??? Remove]   ?
???????????????????????????????????????????????
```

## Conclusion

The Copy Activity feature is now fully implemented and tested, providing facilitators with a quick way to duplicate activities. Edit buttons are now prominently displayed on both pages, making it easier to modify activities during session preparation and execution.
