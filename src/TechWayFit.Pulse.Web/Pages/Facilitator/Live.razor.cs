using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TechWayFit.Pulse.Contracts.Enums;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Components.Facilitator;
using TechWayFit.Pulse.Web.Hubs;
using TechWayFit.Pulse.Web.Services;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Api;
using System.Text.Json;

namespace TechWayFit.Pulse.Web.Pages.Facilitator;

public partial class Live : IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private IActivityService ActivityService { get; set; } = default!;
    [Inject] private IParticipantService ParticipantService { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private IClientTokenService TokenService { get; set; } = default!;
    [Inject] private ILogger<Live> Logger { get; set; } = default!;
    [Inject] private IWebHostEnvironment Environment { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Code { get; set; }

    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    private string sessionCode = string.Empty;
    private SessionSummaryResponse? session;
    private List<AgendaActivityResponse> activities = new();
    private AgendaActivityResponse? currentActivity;
    private int participantCount = 0;
    private string joinUrl = string.Empty;
    private bool isLoading = true;
    private bool isPerformingAction = false;
    private bool isGeneratingQR = false;
    private string errorMessage = string.Empty;
    private HubConnection? hubConnection;
    private JsonElement _aiInsightJson;
    private bool _hasAiInsight = false;
    private DateTimeOffset _aiInsightTimestamp;
    private int timerDurationInput = 5;
    private string activityModalsHtml = string.Empty;

    // Modal component references
    private EditActivityModal? editActivityModal;

    protected override async Task OnInitializedAsync()
    {
        await LoadActivityModalsHtml();
        await LoadSession();
        await EnsureFacilitatorAuthentication();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializePageAsync();
        }
    }

    private async Task InitializeActivityManager()
    {
        try
        {
            if (string.IsNullOrEmpty(sessionCode))
            {
                Logger.LogWarning("Cannot initialize activity manager - session code is empty");
                return;
            }

            // Get the facilitator token
            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                Logger.LogWarning("No facilitator token available for session {SessionCode}", sessionCode);
            }

            Logger.LogDebug("Initializing activity manager with session code: {SessionCode}", sessionCode);
            await JS.InvokeVoidAsync("initializeLiveActivityManager", sessionCode, token ?? "");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize activity manager");
        }
    }

    private async Task InitializePageAsync()
    {
        // Set page title
        await JS.InvokeVoidAsync("eval", "document.title = 'TechWayFit Pulse - Live Session'");

        // Initialize activity timer
        await JS.InvokeVoidAsync("eval", @"
            window.initActivityTimer = function() {
         let timerInterval = null;

 function startActivityTimer() {
 console.log('startActivityTimer called');
     const timerEl = document.getElementById('activity-timer');
     console.log('Timer element:', timerEl);
        
             if (!timerEl) {
              console.log('No timer element found');
           return;
      }

      const durationMinutes = parseInt(timerEl.dataset.duration);
        const openedAt = new Date(timerEl.dataset.openedAt);
                 const timerText = timerEl.querySelector('.timer-text');

         console.log('Duration:', durationMinutes, 'OpenedAt:', openedAt, 'TimerText:', timerText);

       if (timerInterval) clearInterval(timerInterval);

         function updateTimer() {
            const now = new Date();
         const elapsedMs = now - openedAt;
      const elapsedSeconds = Math.floor(elapsedMs / 1000);
            const totalSeconds = durationMinutes * 60;
         const remainingSeconds = Math.max(0, totalSeconds - elapsedSeconds);

        const minutes = Math.floor(remainingSeconds / 60);
    const seconds = remainingSeconds % 60;

      timerText.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

      // Visual feedback
      if (remainingSeconds === 0) {
        timerEl.classList.add('timer-expired');
        timerText.textContent = 'Times Up!';
    } else if (remainingSeconds <= 60) {
        timerEl.classList.add('timer-warning');
               timerEl.classList.remove('timer-expired');
         } else {
            timerEl.classList.remove('timer-warning', 'timer-expired');
           }
     }

         updateTimer();
          timerInterval = setInterval(updateTimer, 1000);
           }

     // Auto-start timer
    startActivityTimer();

           // Restart timer on Blazor re-render (only when element is added)
     let lastTimerElement = document.getElementById('activity-timer');
        const observer = new MutationObserver(() => {
               const currentTimerElement = document.getElementById('activity-timer');
            if (currentTimerElement && !lastTimerElement) {
      startActivityTimer();
    } else if (!currentTimerElement && timerInterval) {
         clearInterval(timerInterval);
   timerInterval = null;
      }
            lastTimerElement = currentTimerElement;
          });

    observer.observe(document.body, {
        childList: true,
        subtree: true
       });
   };
            
        window.initActivityTimer();
        ");
    }

    #region Activity Modals Loading

    private async Task LoadActivityModalsHtml()
    {
        try
        {
            var httpClient = HttpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(Navigation.BaseUri);
            var response = await httpClient.GetAsync("/facilitator/activity-modals");

            if (response.IsSuccessStatusCode)
            {
                activityModalsHtml = await response.Content.ReadAsStringAsync();
                Logger.LogDebug("Loaded activity modals HTML ({Length} characters)", activityModalsHtml.Length);
            }
            else
            {
                Logger.LogWarning("Failed to load activity modals: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading activity modals HTML");
        }
    }

    #endregion

    #region Authentication

    private async Task EnsureFacilitatorAuthentication()
    {
        try
        {
            // Check if token was provided in query parameter first
            if (!string.IsNullOrEmpty(Token))
            {
                await TokenService.StoreFacilitatorTokenAsync(sessionCode, Token);
                Logger.LogDebug("Stored facilitator token from query parameter for session {SessionCode}", sessionCode);
                return;
            }

            // Try to get a token (this will automatically join as facilitator if needed)
            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (!string.IsNullOrEmpty(token))
            {
                Logger.LogInformation("Successfully obtained facilitator token for session {SessionCode}", sessionCode);
            }
            else
            {
                Logger.LogError("Failed to obtain facilitator token for session {SessionCode}", sessionCode);
                errorMessage = "Failed to authenticate as facilitator. Please refresh and try again.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to ensure facilitator authentication for session {SessionCode}", sessionCode);
            errorMessage = $"Authentication error: {ex.Message}. Please refresh and try again.";
        }
    }

    #endregion

    #region Session Loading

    private async Task LoadSession()
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;
            StateHasChanged();

            // Get session code from query parameter
            if (string.IsNullOrWhiteSpace(Code))
            {
                errorMessage = "No session code provided. Please create a session first.";
                isLoading = false;
                StateHasChanged();
                return;
            }

            // Check for facilitator token (optional for now - TODO: implement proper auth)
            if (string.IsNullOrWhiteSpace(Token))
            {
                Logger.LogWarning("Live page accessed without facilitator token for session {SessionCode}", Code);
                // Continue loading the session but disable certain actions
            }

            sessionCode = Code;

            // Load session data
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                isLoading = false;
                StateHasChanged();
                return;
            }
            session = ApiMapper.ToSummary(sessionEntity);

            // Load activities
            var activityEntities = await ActivityService.GetAgendaAsync(sessionEntity.Id);
            activities = activityEntities.Select(ApiMapper.ToAgenda).ToList();
            currentActivity = activities.FirstOrDefault(a => a.Status == ActivityStatus.Open);

            Logger.LogInformation("LoadSession completed - currentActivity: {Title}, DurationMinutes: {Duration}, OpenedAt: {OpenedAt}",
     currentActivity?.Title, currentActivity?.DurationMinutes, currentActivity?.OpenedAt);

            // Load initial participant count
            try
            {
                var participants = await ParticipantService.GetBySessionAsync(sessionEntity.Id);
                participantCount = participants.Count;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load initial participant count, defaulting to 0");
                participantCount = 0; // Will be updated via SignalR events
            }

            // Build join URL
            var uri = new Uri(Navigation.Uri);
            joinUrl = $"{uri.Scheme}://{uri.Authority}/participant/join?sessionCode={sessionCode}";

            // Setup SignalR connection for real-time updates
            await SetupSignalRConnection();

            // Initialize activity manager now that session code is set
            await InitializeActivityManager();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load session: {ex.Message}";
            Logger.LogError(ex, "Failed to load session {Code}", Code);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    #endregion

    #region QR Code and Joining

    private async Task GenerateQRCode()
    {
        try
        {
            var canvasId = $"qr-{sessionCode.Replace("-", "")}";
            await JS.InvokeVoidAsync("generateQRCode", canvasId, joinUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate QR code");
            errorMessage = $"QR generation failed: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task ShowQRModal()
    {
        try
        {
            // Open the modal
            await JS.InvokeVoidAsync("eval", "new bootstrap.Modal(document.getElementById('qrModal')).show()");

            // Wait a bit for modal to render
            await Task.Delay(100);

            // Generate QR code in the modal
            var canvasId = $"qr-modal-{sessionCode.Replace("-", "")}";
            await JS.InvokeVoidAsync("generateQRCode", canvasId, joinUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to show QR modal");
        }
    }

    private async Task CopyJoinUrl()
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", joinUrl);
            // Optional: Show success feedback
            Logger.LogInformation("Join URL copied to clipboard");
        }
        catch (Exception ex)
        {
            // Fallback: Try using a different clipboard method
            try
            {
                await JS.InvokeVoidAsync("copyToClipboard", joinUrl);
                Logger.LogInformation("Join URL copied using fallback method");
            }
            catch
            {
                Logger.LogWarning($"Copy failed: {ex.Message}");
            }
        }
    }

    private async Task ManualGenerateQR()
    {
        try
        {
            isGeneratingQR = true;
            StateHasChanged();

            errorMessage = string.Empty;
            await Task.Delay(100);

            var canvasId = $"qr-{sessionCode.Replace("-", "")}";
            await JS.InvokeVoidAsync("generateQRCode", canvasId, joinUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Manual QR generation failed");
            errorMessage = $"Failed to generate QR code: {ex.Message}";
        }
        finally
        {
            isGeneratingQR = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Session Management

    private async Task EndSession()
    {
        try
        {
            isPerformingAction = true;
            StateHasChanged();

            // Get facilitator token from token service
            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                StateHasChanged();
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                StateHasChanged();
                return;
            }

            // End session
            await SessionService.SetStatusAsync(sessionEntity.Id, TechWayFit.Pulse.Domain.Enums.SessionStatus.Ended, DateTimeOffset.UtcNow);

            // Refresh session
            sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity != null)
            {
                session = ApiMapper.ToSummary(sessionEntity);
            }

            // Session ended successfully - redirect to facilitator dashboard
            Navigation.NavigateTo("/facilitator/dashboard");
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to end session: {ex.Message}";
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    private async Task StartSession()
    {
        try
        {
            isPerformingAction = true;
            StateHasChanged();

            // Get facilitator token from token service
            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                StateHasChanged();
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                StateHasChanged();
                return;
            }

            // Start session
            await SessionService.SetStatusAsync(sessionEntity.Id, TechWayFit.Pulse.Domain.Enums.SessionStatus.Live, DateTimeOffset.UtcNow);

            // Refresh session
            sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity != null)
            {
                session = ApiMapper.ToSummary(sessionEntity);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to start session: {ex.Message}";
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    private string GetSessionStatusClass()
    {
        return session?.Status switch
        {
            SessionStatus.Draft => "bg-warning bg-opacity-10 text-warning border border-warning border-opacity-25",
            SessionStatus.Live => "bg-success bg-opacity-10 text-success border border-success border-opacity-25",
            SessionStatus.Ended => "bg-secondary bg-opacity-10 text-secondary border border-secondary border-opacity-25",
            SessionStatus.Expired => "bg-danger bg-opacity-10 text-danger border border-danger border-opacity-25",
            _ => "bg-light text-dark border"
        };
    }

    #endregion

    #region Activity Management

    private bool HasPreviousActivity()
    {
        if (currentActivity == null) return false;
        return activities.Any(a => a.Order < currentActivity.Order && a.Status != ActivityStatus.Open);
    }

    private bool HasNextActivity()
    {
        if (currentActivity == null) return false;
        return activities.Any(a => a.Order > currentActivity.Order);
    }

    private int GetCurrentActivityResponseCount()
    {
        // TODO: Implement actual response count from API
        // For now, return 0 - will be updated via SignalR
        return 0;
    }

    private async Task OpenActivity(Guid activityId)
    {
        try
        {
            isPerformingAction = true;
            errorMessage = string.Empty;
            StateHasChanged();

            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                return;
            }

            await ActivityService.OpenAsync(sessionEntity.Id, activityId, DateTimeOffset.UtcNow);

            // Reload session to get updated activity status
            await LoadSession();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to open activity: {ex.Message}";
            Logger.LogError(ex, "Failed to open activity {ActivityId}", activityId);
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    private async Task CloseCurrentActivity()
    {
        if (currentActivity == null) return;

        try
        {
            isPerformingAction = true;
            errorMessage = string.Empty;
            StateHasChanged();

            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                return;
            }

            await ActivityService.CloseAsync(sessionEntity.Id, currentActivity.ActivityId, DateTimeOffset.UtcNow);

            // Reload session to get updated activity status
            await LoadSession();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to close activity: {ex.Message}";
            Logger.LogError(ex, "Failed to close activity {ActivityId}", currentActivity?.ActivityId);
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    private async Task MoveNext()
    {
        if (currentActivity == null) return;

        isPerformingAction = true;
        errorMessage = string.Empty;

        try
        {
            // 1. Find next pending activity
            var nextActivity = activities
           .Where(a => a.Order > currentActivity.Order && a.Status == ActivityStatus.Pending)
           .OrderBy(a => a.Order)
                 .FirstOrDefault();

            if (nextActivity == null)
            {
                errorMessage = "No more activities to move to.";
                return;
            }

            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                return;
            }

            // 2. Close current activity
            await ActivityService.CloseAsync(sessionEntity.Id, currentActivity.ActivityId, DateTimeOffset.UtcNow);

            // 3. Open next activity
            await ActivityService.OpenAsync(sessionEntity.Id, nextActivity.ActivityId, DateTimeOffset.UtcNow);

            // 4. Reload session
            await LoadSession();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to move to next activity: {ex.Message}";
            Logger.LogError(ex, "Failed to move to next activity");
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    private async Task GoBack()
    {
        if (currentActivity == null) return;

        isPerformingAction = true;
        errorMessage = string.Empty;

        try
        {
            // 1. Find previous activity (either pending or closed)
            var previousActivity = activities
                .Where(a => a.Order < currentActivity.Order)
      .OrderByDescending(a => a.Order)
         .FirstOrDefault();

            if (previousActivity == null)
            {
                errorMessage = "No previous activity to go back to.";
                return;
            }

            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                return;
            }

            // 2. Close current activity
            await ActivityService.CloseAsync(sessionEntity.Id, currentActivity.ActivityId, DateTimeOffset.UtcNow);

            // 3. Open previous activity
            await ActivityService.OpenAsync(sessionEntity.Id, previousActivity.ActivityId, DateTimeOffset.UtcNow);

            // 4. Reload session
            await LoadSession();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to go back: {ex.Message}";
            Logger.LogError(ex, "Failed to go back to previous activity");
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    private async Task ReopenActivity(Guid activityId)
    {
        // Cannot reopen if session has ended
        if (session?.Status == SessionStatus.Ended || session?.Status == SessionStatus.Expired)
        {
            errorMessage = "Cannot reopen activities. The session has ended.";
            StateHasChanged();
            return;
        }

        // Close current activity if one is open
        if (currentActivity != null)
        {
            try
            {
                var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
                if (string.IsNullOrEmpty(token))
                {
                    errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                    StateHasChanged();
                    return;
                }

                var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
                if (sessionEntity == null)
                {
                    errorMessage = "Session not found.";
                    StateHasChanged();
                    return;
                }
                await ActivityService.CloseAsync(sessionEntity.Id, currentActivity.ActivityId, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to close current activity: {ex.Message}";
                Logger.LogError(ex, "Failed to close current activity {ActivityId} before reopening", currentActivity.ActivityId);
                StateHasChanged();
                return;
            }
        }

        try
        {
            isPerformingAction = true;
            errorMessage = string.Empty;
            StateHasChanged();

            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                return;
            }

            await ActivityService.ReopenAsync(sessionEntity.Id, activityId, DateTimeOffset.UtcNow);

            // Reload session to get updated activity status
            await LoadSession();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to reopen activity: {ex.Message}";
            Logger.LogError(ex, "Failed to reopen activity {ActivityId}", activityId);
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Timer Management

    private async Task ShowSetTimerModal()
    {
        // Reset input to current duration or default
        timerDurationInput = currentActivity?.DurationMinutes ?? 5;
        StateHasChanged();

        // Show modal using Bootstrap
        await JS.InvokeVoidAsync("eval", "new bootstrap.Modal(document.getElementById('setTimerModal')).show()");
    }

    private async Task SetActivityTimer(int minutes)
    {
        if (currentActivity == null) return;

        try
        {
            isPerformingAction = true;
            errorMessage = string.Empty;
            StateHasChanged();

            var token = await TokenService.GetFacilitatorTokenAsync(sessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            // Get session entity
            var sessionEntity = await SessionService.GetByCodeAsync(sessionCode);
            if (sessionEntity == null)
            {
                errorMessage = "Session not found.";
                return;
            }

            // Update the activity with the new duration
            await ActivityService.UpdateActivityAsync(
                sessionEntity.Id,
                currentActivity.ActivityId,
                currentActivity.Title,
                currentActivity.Prompt,
                currentActivity.Config,
                minutes);

            // Give the database a moment to commit the transaction
            await Task.Delay(100);

            // Reload session to get updated activity
            await LoadSession();

            Logger.LogInformation("After LoadSession - currentActivity.DurationMinutes: {Duration}, OpenedAt: {OpenedAt}",
                   currentActivity?.DurationMinutes, currentActivity?.OpenedAt);

            // Close modal and remove backdrop
            await JS.InvokeVoidAsync("eval", @"
            var modalEl = document.getElementById('setTimerModal');
        var modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
   modal.hide();
       // Remove backdrop manually
       setTimeout(() => {
       document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
 document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
          document.body.style.removeProperty('padding-right');
       }, 200);
 ");

            // Wait for modal to close and Blazor to re-render, then start timer
            await Task.Delay(300);
            StateHasChanged();
            await Task.Delay(100);
            await JS.InvokeVoidAsync("eval", @"
       console.log('Attempting to start timer...');
              var timerEl = document.getElementById('activity-timer');
                console.log('Timer element:', timerEl);
    if (timerEl) {
 console.log('Timer data - duration:', timerEl.dataset.duration, 'openedAt:', timerEl.dataset.openedAt);
    }
    if (typeof startActivityTimer === 'function') {
        startActivityTimer();
            } else {
 console.log('startActivityTimer function not found');
    }
");
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to set timer: {ex.Message}";
            Logger.LogError(ex, "Failed to set timer for activity {ActivityId}", currentActivity.ActivityId);
        }
        finally
        {
            isPerformingAction = false;
            StateHasChanged();
        }
    }

    #endregion

    #region SignalR

    private async Task SetupSignalRConnection()
    {
        try
        {
            hubConnection = new HubConnectionBuilder()
          .WithUrl(Navigation.ToAbsoluteUri("/hubs/workshop"))
                .Build();

            // Handle session state changes (includes participant count)
            hubConnection.On<SessionStateChangedEvent>("SessionStateChanged", async (sessionEvent) =>
  {
      await InvokeAsync(async () =>
        {
            if (session != null && sessionEvent.SessionCode == session.Code)
            {
                participantCount = sessionEvent.ParticipantCount;
                StateHasChanged();
            }
        });
  });

            // Handle participant joins
            hubConnection.On<ParticipantJoinedEvent>("ParticipantJoined", async (participantEvent) =>
        {
            await InvokeAsync(() =>
{
              if (session != null && participantEvent.SessionCode == session.Code)
              {
                  participantCount = participantEvent.TotalParticipantCount;
                  StateHasChanged();
              }
          });
        });

            // Handle response received events (for real-time response tracking)
            hubConnection.On<ResponseReceivedEvent>("ResponseReceived", async (responseEvent) =>
                    {
                        await InvokeAsync(() =>
              {
                if (session != null && responseEvent.SessionCode == session.Code)
                {
                      // Trigger UI refresh - the PollDashboard component handles its own SignalR updates
                    if (currentActivity != null &&
     responseEvent.ActivityId == currentActivity.ActivityId)
                    {
                        StateHasChanged();
                    }
                }
            });
                    });

            // Handle dashboard update events (for real-time dashboard updates)
            hubConnection.On<DashboardUpdatedEvent>("DashboardUpdated", async (dashboardEvent) =>
           {
               await InvokeAsync(() =>
{
                      if (session != null && dashboardEvent.SessionCode == session.Code)
                      {
        // If this is an AI insight, capture payload for facilitator UI
                          try
                          {
                              if (string.Equals(dashboardEvent.AggregateType, "AIInsight", StringComparison.OrdinalIgnoreCase))
                              {
                                  if (dashboardEvent.Payload is JsonElement je)
                                  {
                                      _aiInsightJson = je;
                                  }
                                  else
                                  {
                    // Try to serialize/deserialize generic object into JsonElement
                                      _aiInsightJson = JsonSerializer.SerializeToElement(dashboardEvent.Payload ?? new { });
                                  }
                                  _hasAiInsight = true;
                                  _aiInsightTimestamp = dashboardEvent.Timestamp;
                              }

            // Trigger UI refresh - the dashboard components handle their own updates
                              if (currentActivity != null && dashboardEvent.ActivityId.HasValue && dashboardEvent.ActivityId == currentActivity.ActivityId)
                              {
                                  StateHasChanged();
                              }
                          }
                          catch
                          {
            // ignore any payload parsing issues
                          }
                      }
                  });
           });

            // Handle activity deleted events
            hubConnection.On<Guid>("ActivityDeleted", async (activityId) =>
            {
                await InvokeAsync(() =>
                        {
                            // Remove the deleted activity from the list
                            activities.RemoveAll(a => a.ActivityId == activityId);

                            // If the deleted activity was the current one, clear it
                            if (currentActivity?.ActivityId == activityId)
                            {
                                currentActivity = null;
                            }

                            StateHasChanged();
                        });
            });

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("Subscribe", sessionCode);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to setup SignalR connection");
        }
    }

    #endregion

    #region Modal Handlers

    private void ShowEditSessionModal()
    {
        var returnUrl = $"/facilitator/live?Code={Uri.EscapeDataString(sessionCode)}";
        Navigation.NavigateTo($"/facilitator/edit-session/{sessionCode}?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }

    private void ShowEditActivityModal(AgendaActivityResponse activity)
    {
        if (editActivityModal != null)
        {
            editActivityModal.Activity = activity;
            editActivityModal.Show();
        }
    }

    private async Task ShowAddActivityModal(string activityType)
    {
        // Modal is opened via HTML data-bs-toggle, this just logs
        Logger.LogDebug("Add activity modal triggered for type: {ActivityType}", activityType);
        await Task.CompletedTask;
    }

    private async Task HandleSessionUpdated()
    {
        await LoadSession();
    }

    private async Task HandleActivityUpdated()
    {
        await LoadSession();
    }

    private async Task HandleActivityDeleted()
    {
        await LoadSession();
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            try
            {
                await hubConnection.InvokeAsync("Unsubscribe", sessionCode);
                await hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error disposing SignalR connection");
            }
        }
    }

    #endregion
}
