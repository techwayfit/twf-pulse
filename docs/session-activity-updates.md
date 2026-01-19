# Session and Activity Update/Edit Functionality

## Overview
This document describes the capability to edit/update sessions and activities after they have been created.

## Session Updates

### Update Session Metadata
**Endpoint:** `PUT /api/sessions/{code}`

Updates session title, goal, and context.

**Request Body:**
```json
{
  "title": "Updated Session Title",
  "goal": "Updated goal description",
  "context": "Updated context information"
}
```

**Validation:**
- Title is required (max 200 characters)
- Requires facilitator authentication token
- Returns updated session summary

**Domain Method:**
```csharp
public void Update(string title, string? goal, string? context, DateTimeOffset updatedAt)
```

---

### Update Session Settings
**Endpoint:** `PUT /api/sessions/{code}/settings`

Updates session configuration settings.

**Request Body:**
```json
{
  "maxContributionsPerParticipantPerSession": 0,
  "maxContributionsPerParticipantPerActivity": 0,
  "strictCurrentActivityOnly": true,
  "allowAnonymous": false,
  "ttlMinutes": 120
}
```

**Validation:**
- Requires facilitator authentication token
- Returns updated session summary

**Domain Method:**
```csharp
public void UpdateSettings(SessionSettings settings, DateTimeOffset updatedAt)
```

---

## Activity Updates

### Update Activity Details
**Endpoint:** `PUT /api/sessions/{code}/activities/{activityId}`

Updates activity title, prompt, and configuration.

**Request Body:**
```json
{
  "title": "Updated Activity Title",
  "prompt": "Updated activity prompt",
  "config": "{\"key\": \"value\"}"
}
```

**Validation:**
- Title is required (max 200 characters)
- Prompt max 1000 characters
- Requires facilitator authentication token
- Publishes `ActivityStateChanged` SignalR event after update

**Domain Method:**
```csharp
public void Update(string title, string? prompt, string? config)
```

---

### Delete Activity
**Endpoint:** `DELETE /api/sessions/{code}/activities/{activityId}`

Deletes a pending activity from the session.

**Business Rules:**
- Only activities with status `Pending` can be deleted
- Cannot delete activities that are `InProgress` or `Completed`

**Validation:**
- Activity must exist and belong to the session
- Activity must be in `Pending` status
- Requires facilitator authentication token
- Publishes `ActivityDeleted` SignalR event

**Response:**
```json
{
  "success": true,
  "data": {
    "message": "Activity deleted successfully"
  }
}
```

---

## Implementation Details

### Service Layer
All update operations are implemented in:
- `ISessionService.UpdateSessionAsync()`
- `ISessionService.UpdateSessionSettingsAsync()`
- `IActivityService.UpdateActivityAsync()`
- `IActivityService.DeleteActivityAsync()`

### Repository Layer
Added to `IActivityRepository`:
```csharp
Task DeleteAsync(Activity activity, CancellationToken cancellationToken = default);
```

### SignalR Events
New client method added to `IWorkshopClient`:
```csharp
Task ActivityDeleted(Guid activityId);
```

This allows real-time notification to all connected clients when an activity is deleted.

---

## Authentication
All update/delete endpoints require facilitator authentication:
- Facilitator token must be provided via Cookie or Authorization header
- Token must match the session's facilitator token
- Unauthorized requests return `401 Unauthorized`

---

## Error Handling
All endpoints return consistent error responses:

**Validation Error (400 Bad Request):**
```json
{
  "success": false,
  "error": {
    "code": "validation_error",
    "message": "Title is required and cannot exceed 200 characters."
  }
}
```

**Not Found (404):**
```json
{
  "success": false,
  "error": {
    "code": "not_found",
    "message": "Session not found."
  }
}
```

**Unauthorized (401):**
```json
{
  "success": false,
  "error": {
    "code": "unauthorized",
    "message": "Valid facilitator token required."
  }
}
```

---

## Next Steps: UI Implementation

### 1. Template Customization UI
Create a page/component to:
- Browse available templates
- Preview template details (activities, settings)
- Customize template values before creating session
- Select session group folder
- Provide custom session name

### 2. Session Edit UI
Create a page/component to:
- Edit session metadata (title, goal, context)
- Edit session settings
- View current values
- Save changes via PUT endpoints

### 3. Activity Edit UI
Create a page/component to:
- Edit activity details (title, prompt, config)
- Delete pending activities
- Reorder activities (already has API)
- Disable edit/delete for non-pending activities

### 4. Real-time Updates
Implement SignalR client handlers:
- Listen for `ActivityDeleted` events
- Update UI when activities are modified
- Refresh activity list when changes occur

---

## Related Documentation
- [Session Templates](./session-templates.md) - Template framework overview
- [Session Template Implementation Summary](./session-template-implementation-summary.md) - Template implementation details
- [API Documentation] - Complete API endpoint reference
