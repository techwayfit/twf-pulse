# Integration Guide: Activity Components in Activity.razor

## Overview
This guide shows **exactly** how to integrate the new activity components into `Activity.razor`.

## Current Issues in Activity.razor

The current `Activity.razor` file has **compilation errors** that need to be fixed:
- Duplicate `class` attributes on buttons (lines 603, 789)
- Duplicate `maxlength` attributes on textareas
- Duplicate class properties in config classes

## Step-by-Step Integration

### Step 1: Add Using Directive

Add this at the top of `Activity.razor`:

```razor
@using TechWayFit.Pulse.Web.Components.Participant/Activities
```

### Step 2: Add Helper Method in @code Block

Add this method to the `@code` block:

```csharp
private ParticipantActivityParameters CreateActivityParameters()
{
    return new ParticipantActivityParameters
    {
        SessionCode = SessionCode,
        ParticipantId = Pid!.Value,
        ActivityId = currentActivity!.ActivityId,
        Config = currentActivity.Config ?? "{}",
   HasSubmitted = hasSubmittedResponse,
        IsSubmitting = isSubmittingResponse,
   SubmitMessage = submitMessage,
  SubmitSuccess = submitSuccess,
        OnSubmit = SubmitResponse  // This already exists!
    };
}
```

### Step 3: Replace Activity Rendering Logic

**Find this code** (around line 121-139):

```razor
@if (currentActivity.Status == TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Open)
{
    <div class="response-section mt-4">
        @if (currentActivity.Type == ActivityType.Poll)
  {
            @RenderPollActivity()
        }
      else if (currentActivity.Type == ActivityType.WordCloud)
        {
            @RenderWordCloudActivity()
   }
        else if (currentActivity.Type == ActivityType.Rating)
{
         @RenderRatingActivity()
        }
        else if (currentActivity.Type == ActivityType.GeneralFeedback)
        {
       @RenderGeneralFeedbackActivity()
  }
        else
        {
         @RenderGenericActivity()
        }
    </div>
}
```

**Replace with**:

```razor
@if (currentActivity.Status == TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Open)
{
    <div class="response-section mt-4">
  @if (currentActivity.Type == ActivityType.Poll)
     {
<PollActivity Parameters="@CreateActivityParameters()" />
}
        else if (currentActivity.Type == ActivityType.WordCloud)
 {
       <WordCloudActivity Parameters="@CreateActivityParameters()" />
}
        else if (currentActivity.Type == ActivityType.Rating)
  {
         <RatingActivity Parameters="@CreateActivityParameters()" />
        }
        else if (currentActivity.Type == ActivityType.GeneralFeedback)
        {
   <GeneralFeedbackActivity Parameters="@CreateActivityParameters()" />
    }
        else
        {
            <GenericActivity Parameters="@CreateActivityParameters()" ActivityType="@currentActivity.Type.ToString()" />
        }
    </div>
}
```

### Step 4: Remove Old Methods and State

**Delete these entire methods** (they're now in the components):

1. `RenderPollActivity()` - Now in `PollActivity.razor`
2. `RenderWordCloudActivity()` - Now in `WordCloudActivity.razor`
3. `RenderRatingActivity()` - Now in `RatingActivity.razor`
4. `RenderGeneralFeedbackActivity()` - Now in `GeneralFeedbackActivity.razor`
5. `RenderGenericActivity()` - Now in `GenericActivity.razor`

**Delete these Poll-specific methods**:
- `SelectPollOption(string optionId)`
- `TogglePollOption(string optionId)`
- `HandlePollOptionClick(string optionId, bool allowMultiple)`
- `HandleSubmitPollResponse()`
- `SubmitPollResponse()`

**Delete these WordCloud-specific methods**:
- `OnWordCloudTextChanged(ChangeEventArgs e)`
- `SubmitWordCloudResponse()`
- `IsWordCloudInputValid()`
- `GetWordCloudValidationMessage()`

**Delete these Rating-specific methods**:
- `SetRating(int rating)`
- `SubmitRatingResponse()`

**Delete these Feedback-specific methods**:
- `OnFeedbackTextChanged(ChangeEventArgs e)`
- `IsGeneralFeedbackInvalid()`
- `SubmitGeneralFeedbackResponse()`

**Delete these Generic-specific methods**:
- `OnGenericTextChanged(ChangeEventArgs e)`
- `SubmitGenericResponse()`

**Delete these state variables** (no longer needed in parent):
```csharp
// Poll activity state
private List<string> selectedPollOptionIds = new();
private string? customPollText;
private PollConfig? pollConfig;

// WordCloud state
private string? wordCloudText;
private WordCloudConfig? wordCloudConfig;

// Rating state
private int selectedRating = 0;
private RatingConfig? ratingConfig;
private string? ratingComment;

// GeneralFeedback state
private string? feedbackText;
private string? selectedFeedbackCategory;
private GeneralFeedbackConfig? feedbackConfig;

// Generic state
private string? genericResponseText;
```

**Delete these config classes** (they're in Domain project):
```csharp
private class PollConfig { ... }
private class PollOption { ... }
private class GeneralFeedbackConfig { ... }
private class FeedbackCategory { ... }
private class RatingConfig { ... }
```

### Step 5: Update ParseActivityConfig()

**Replace the entire method** with:

```csharp
private void ParseActivityConfig()
{
    // No longer needed - each component parses its own config
    // Keep this method empty or remove it entirely
}
```

### Step 6: Update ResetFormFields()

**Replace with**:

```csharp
private void ResetFormFields()
{
    // No longer needed - each component manages its own state
    // Keep this method empty or just remove the call to it
}
```

### Step 7: Update GetSubmitButtonText()

**Keep this method** - it's still used by the parent component for consistency:

```csharp
private string GetSubmitButtonText()
{
    if (hasSubmittedResponse)
        return "Response Submitted ?";
    if (isSubmittingResponse)
  return "Submitting...";
    return "Submit Response";
}
```

## What Stays in Activity.razor

The parent component still handles:

? **Session Management**
- Loading session data
- SignalR connection
- Session status display

? **Response Submission**
- `SubmitResponse(string payload)` - Centralized API call
- Submission state management (`isSubmittingResponse`, `hasSubmittedResponse`, etc.)

? **Activity Routing**
- Determining which component to display
- Passing parameters to components

? **Global UI**
- Session header
- Status badges
- Error messages
- Loading states

## What Moved to Components

? **Activity-Specific Logic** (moved to individual components)
- Configuration parsing
- Input validation
- UI rendering
- State management
- Payload formatting

## Size Comparison

### Before
- **Activity.razor**: ~1200 lines
- Everything in one file
- Hard to navigate
- Difficult to test

### After
- **Activity.razor**: ~450 lines (orchestration only)
- **PollActivity.razor**: ~175 lines
- **WordCloudActivity.razor**: ~165 lines
- **RatingActivity.razor**: ~195 lines
- **GeneralFeedbackActivity.razor**: ~185 lines
- **GenericActivity.razor**: ~95 lines
- **Total**: ~1265 lines (but organized!)

**Net benefit**: Same functionality, much better organization!

## Testing the Integration

After making changes, test:

1. **Build the project**
   ```bash
   dotnet build
   ```

2. **Test each activity type**:
   - [ ] Create a Poll activity - verify submission works
   - [ ] Create a WordCloud activity - test validation
   - [ ] Create a Rating activity - test different display modes
   - [ ] Create a GeneralFeedback activity - test categories
 - [ ] Test an unsupported type - verify GenericActivity renders

3. **Test state transitions**:
   - [ ] Open activity ? Input ? Submit ? Success
   - [ ] Activity changes ? State resets
   - [ ] SignalR updates ? Component re-renders

4. **Test error handling**:
   - [ ] Network error during submission
   - [ ] Invalid configuration JSON
   - [ ] Missing required fields

## Common Issues & Solutions

### Issue: "The name 'PollActivity' does not exist"
**Solution**: Add `@using TechWayFit.Pulse.Web.Components.Participant.Activities` at the top

### Issue: "Cannot convert from 'method group' to 'ActivitySubmittedCallback'"
**Solution**: Use `OnSubmit = SubmitResponse` (not `OnSubmit = SubmitResponse()`)

### Issue: Components don't update after submission
**Solution**: Ensure `SubmitResponse` method updates `hasSubmittedResponse`, `submitSuccess`, etc.

### Issue: Configuration not parsing
**Solution**: Verify the JSON in `currentActivity.Config` matches the expected format

## Rollback Plan

If integration fails:
1. Revert `Activity.razor` to previous version
2. Keep the new component files (they're standalone)
3. Debug issues in isolation
4. Try integration again

## Next Steps After Integration

1. ? Verify all builds successfully
2. ? Test each activity type manually
3. ? Update unit tests if needed
4. ? Update documentation
5. ? Consider adding component-level tests

---

**Ready to integrate?** Follow the steps above in order. Each step is independent and can be done incrementally!
