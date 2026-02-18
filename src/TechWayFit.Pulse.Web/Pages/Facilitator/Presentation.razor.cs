using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Hubs;
using TechWayFit.Pulse.Web.Services;

namespace TechWayFit.Pulse.Web.Pages.Facilitator;

public partial class Presentation : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private IActivityService ActivityService { get; set; } = default!;
    [Inject] private IParticipantService ParticipantService { get; set; } = default!;
    [Inject] private IClientTokenService TokenService { get; set; } = default!;
    [Inject] private ILogger<Presentation> Logger { get; set; } = default!;
    [Inject] private TechWayFit.Pulse.Web.Services.IHubNotificationService HubNotifications { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Code { get; set; }

    private string SessionCode => Code ?? string.Empty;

    private AgendaActivityResponse? activity;
    private List<AgendaActivityResponse> activities = new();
    private Session? session;
    private int participantCount = 0;
    private int responseCount = 0;
    private string joinUrl = string.Empty;
    private bool hasPrevious = false;
    private bool hasNext = false;
    private bool isLoading = true;
    private bool isPerformingAction = false;
    private bool isFullscreen = false;
    private bool isReviewNavigation = false;
    private string errorMessage = string.Empty;
    private HubConnection? hubConnection;
    private bool _disposed = false;
    private CancellationTokenSource? _autoFullscreenCts;

    protected override async Task OnInitializedAsync()
    {
        await LoadPresentation();
        await SetupSignalR();
        
        // Try to enter fullscreen on load
        _autoFullscreenCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, _autoFullscreenCts.Token);
                if (!_disposed)
                {
                    await InvokeAsync(async () =>
                    {
                        try
                        {
                            await EnterFullscreen();
                        }
                        catch
                        {
                            // Ignore errors - fullscreen may not be allowed on auto-load
                        }
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Component disposed, ignore
            }
        });
    }

    private async Task LoadPresentation()
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SessionCode))
            {
                errorMessage = "No session code provided.";
                return;
            }

            session = await SessionService.GetByCodeAsync(SessionCode);
            if (session == null)
            {
                errorMessage = "Session not found.";
                return;
            }

            var agenda = await ActivityService.GetAgendaAsync(session.Id);
            activities = agenda
                .Select(ApiMapper.ToAgenda)
                .OrderBy(a => a.Order)
                .ThenBy(a => a.ActivityId)
                .ToList();
            if (activities.Count == 0)
            {
                errorMessage = "No activities found for this session.";
                return;
            }

            var openActivity = activities.FirstOrDefault(a => a.Status == Contracts.Enums.ActivityStatus.Open);

            if (activity == null)
            {
                activity = openActivity ?? activities.First();
            }
            else
            {
                var currentSelection = activities.FirstOrDefault(a => a.ActivityId == activity.ActivityId);
                if (!isReviewNavigation && openActivity != null)
                {
                    activity = openActivity;
                }
                else if (currentSelection != null)
                {
                    activity = currentSelection;
                }
                else
                {
                    activity = openActivity ?? activities.First();
                    isReviewNavigation = false;
                }
            }

            UpdateNavigationFlags();

            try
            {
                var participants = await ParticipantService.GetBySessionAsync(session.Id);
                participantCount = participants.Count;
            }
            catch
            {
                participantCount = 0;
            }

            var uri = new Uri(Navigation.Uri);
            joinUrl = $"{uri.Scheme}://{uri.Authority}/participant/join?sessionCode={SessionCode}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load presentation");
            errorMessage = $"Failed to load presentation: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task SetupSignalR()
    {
        try
        {
            hubConnection = new HubConnectionBuilder()
                  .WithUrl(Navigation.ToAbsoluteUri("/hubs/workshop"))
                     .WithAutomaticReconnect()
                           .Build();

            hubConnection.On<ParticipantJoinedEvent>("ParticipantJoined", async (e) =>
              {
                  await InvokeAsync(() =>
       {
                 if (e.SessionCode == SessionCode)
                 {
                     participantCount = e.TotalParticipantCount;
                     StateHasChanged();
                 }
             });
              });

            hubConnection.On<ResponseReceivedEvent>("ResponseReceived", async (e) =>
                  {
                      await InvokeAsync(() =>
             {
            if (e.SessionCode == SessionCode && e.ActivityId == activity?.ActivityId)
            {
                responseCount++;
                StateHasChanged();
            }
        });
                  });

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("Subscribe", SessionCode);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to setup SignalR");
        }
    }

    private async Task CloseActivity()
    {
        if (activity == null) return;

        try
        {
            isPerformingAction = true;
            var token = await TokenService.GetFacilitatorTokenAsync(SessionCode);
            if (string.IsNullOrEmpty(token)) return;

            if (session != null)
            {
                await ActivityService.CloseAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);

                // Broadcast SignalR event
                var agenda = await ActivityService.GetAgendaAsync(session.Id);
                var closedActivity = agenda.FirstOrDefault(a => a.Id == activity.ActivityId);
                if (closedActivity != null)
                {
                    await HubNotifications.PublishActivityStateChangedAsync(SessionCode, closedActivity);
                }
            }
            Navigation.NavigateTo($"/facilitator/live?code={SessionCode}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to close activity");
            errorMessage = $"Failed to close activity: {ex.Message}";
        }
        finally
        {
            isPerformingAction = false;
        }
    }

    private Task MoveNext()
    {
        if (activity == null) return Task.CompletedTask;

        try
        {
            var currentIndex = GetCurrentActivityIndex();
            if (currentIndex < 0 || currentIndex >= activities.Count - 1)
            {
                return Task.CompletedTask;
            }

            var nextActivity = activities[currentIndex + 1];

            if (nextActivity == null) return Task.CompletedTask;

            activity = nextActivity;
            isReviewNavigation = true;
            errorMessage = string.Empty;
            UpdateNavigationFlags();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to move next");
            errorMessage = $"Failed to move to next activity: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private Task GoBack()
    {
        if (activity == null) return Task.CompletedTask;

        try
        {
            var currentIndex = GetCurrentActivityIndex();
            if (currentIndex <= 0)
            {
                return Task.CompletedTask;
            }

            var previousActivity = activities[currentIndex - 1];

            if (previousActivity == null) return Task.CompletedTask;

            activity = previousActivity;
            isReviewNavigation = true;
            errorMessage = string.Empty;
            UpdateNavigationFlags();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to go back");
            errorMessage = $"Failed to go back: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private async Task ActivateCurrentActivity()
    {
        if (activity == null || session == null)
        {
            return;
        }

        if (activity.Status == Contracts.Enums.ActivityStatus.Open)
        {
            isReviewNavigation = false;
            return;
        }

        try
        {
            isPerformingAction = true;
            errorMessage = string.Empty;

            var token = await TokenService.GetFacilitatorTokenAsync(SessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            var currentlyOpen = activities.FirstOrDefault(a =>
                a.Status == Contracts.Enums.ActivityStatus.Open &&
                a.ActivityId != activity.ActivityId);

            if (currentlyOpen != null)
            {
                await ActivityService.CloseAsync(session.Id, currentlyOpen.ActivityId, DateTimeOffset.UtcNow);
            }

            if (activity.Status == Contracts.Enums.ActivityStatus.Closed)
            {
                await ActivityService.ReopenAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
            }
            else
            {
                await ActivityService.OpenAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
            }

            await SessionService.SetCurrentActivityAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);

            var updatedSession = await SessionService.GetByCodeAsync(SessionCode);
            if (updatedSession != null)
            {
                await HubNotifications.PublishSessionStateChangedAsync(updatedSession);
            }

            var agenda = await ActivityService.GetAgendaAsync(session.Id);
            if (currentlyOpen != null)
            {
                var closedActivity = agenda.FirstOrDefault(a => a.Id == currentlyOpen.ActivityId);
                if (closedActivity != null)
                {
                    await HubNotifications.PublishActivityStateChangedAsync(SessionCode, closedActivity);
                }
            }

            var openedActivity = agenda.FirstOrDefault(a => a.Id == activity.ActivityId);
            if (openedActivity != null)
            {
                await HubNotifications.PublishActivityStateChangedAsync(SessionCode, openedActivity);
            }

            isReviewNavigation = false;
            await LoadPresentation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to activate selected activity");
            errorMessage = $"Failed to activate activity: {ex.Message}";
        }
        finally
        {
            isPerformingAction = false;
        }
    }

    private async Task CompleteCurrentActivity()
    {
        if (activity == null || session == null)
        {
            return;
        }

        if (activity.Status != Contracts.Enums.ActivityStatus.Open)
        {
            return;
        }

        try
        {
            isPerformingAction = true;
            errorMessage = string.Empty;

            var token = await TokenService.GetFacilitatorTokenAsync(SessionCode);
            if (string.IsNullOrEmpty(token))
            {
                errorMessage = "Unable to get facilitator authentication token. Please refresh and try again.";
                return;
            }

            await ActivityService.CloseAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
            await SessionService.SetCurrentActivityAsync(session.Id, null, DateTimeOffset.UtcNow);

            var updatedSession = await SessionService.GetByCodeAsync(SessionCode);
            if (updatedSession != null)
            {
                await HubNotifications.PublishSessionStateChangedAsync(updatedSession);
            }

            var agenda = await ActivityService.GetAgendaAsync(session.Id);
            var closedActivity = agenda.FirstOrDefault(a => a.Id == activity.ActivityId);
            if (closedActivity != null)
            {
                await HubNotifications.PublishActivityStateChangedAsync(SessionCode, closedActivity);
            }

            isReviewNavigation = true;
            await LoadPresentation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to complete selected activity");
            errorMessage = $"Failed to complete activity: {ex.Message}";
        }
        finally
        {
            isPerformingAction = false;
        }
    }

    private void UpdateNavigationFlags()
    {
        if (activity == null)
        {
            hasPrevious = false;
            hasNext = false;
            return;
        }

        var currentIndex = GetCurrentActivityIndex();
        if (currentIndex < 0)
        {
            hasPrevious = false;
            hasNext = false;
            return;
        }

        hasPrevious = currentIndex > 0;
        hasNext = currentIndex < activities.Count - 1;
    }

    private int GetCurrentActivityIndex()
    {
        if (activity == null || activities.Count == 0)
        {
            return -1;
        }

        return activities.FindIndex(a => a.ActivityId == activity.ActivityId);
    }

    private async Task ExitPresenterMode()
    {
        try
        {
            // Exit fullscreen mode first
            await JS.InvokeVoidAsync("eval", "if (document.exitFullscreen) document.exitFullscreen();");

            // Wait a bit for fullscreen to exit
            await Task.Delay(200);
 
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to exit presenter mode");
        } 
            Navigation.NavigateTo($"/facilitator/live?code={SessionCode}",true);
    }

    private async Task ShowQRModal()
    {
        try
        {
            await JS.InvokeVoidAsync("eval", "new bootstrap.Modal(document.getElementById('qrModal')).show()");
            await Task.Delay(100);

            var canvasId = $"qr-modal-{SessionCode.Replace("-", "")}";
            await JS.InvokeVoidAsync("generateQRCode", canvasId, joinUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to show QR modal in presentation mode");
        }
    }

    private async Task CopyJoinUrl()
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", joinUrl);
            Logger.LogInformation("Presentation join URL copied to clipboard");
        }
        catch (Exception ex)
        {
            try
            {
                await JS.InvokeVoidAsync("copyToClipboard", joinUrl);
                Logger.LogInformation("Presentation join URL copied using fallback method");
            }
            catch
            {
                Logger.LogWarning($"Presentation copy failed: {ex.Message}");
            }
        }
    }

    private async Task EnterFullscreen()
    {
        if (_disposed)
        {
            Logger.LogDebug("Component disposed, skipping fullscreen");
            return;
        }

        try
        {
            Logger.LogInformation("EnterFullscreen called");
            
            // Try entering fullscreen
            await JS.InvokeVoidAsync("eval", @"
                const elem = document.documentElement;
                if (elem.requestFullscreen) {
                    elem.requestFullscreen().then(() => {
                        console.log('Fullscreen entered successfully');
                    }).catch(err => {
                        console.log('Fullscreen blocked (expected on auto-load):', err.message);
                    });
                } else if (elem.webkitRequestFullscreen) {
                    elem.webkitRequestFullscreen();
                } else if (elem.msRequestFullscreen) {
                    elem.msRequestFullscreen();
                } else {
                    console.error('Fullscreen API not supported');
                }
            ");
            
            if (_disposed) return;
            
            // Check if we actually entered fullscreen after a short delay
            await Task.Delay(300);
            
            if (_disposed) return;
            
            var isNowFullscreen = await JS.InvokeAsync<bool>("eval", @"
                !!(document.fullscreenElement || 
                   document.webkitFullscreenElement || 
                   document.mozFullScreenElement || 
                   document.msFullscreenElement)
            ");
            
            isFullscreen = isNowFullscreen;
            StateHasChanged();
            
            Logger.LogInformation($"Fullscreen state: {isFullscreen}");
        }
        catch (JSDisconnectedException)
        {
            Logger.LogDebug("Circuit disconnected, cannot enter fullscreen");
            isFullscreen = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to enter fullscreen");
            isFullscreen = false;
            if (!_disposed)
            {
                StateHasChanged();
            }
        }
    }

    // Computed properties for layout
    private string SessionTitle => session?.Title ?? "Workshop Session";
    private string? SessionTTL
    {
        get
        {
            if (session == null) return null;
            var ttl = session.ExpiresAt - DateTimeOffset.UtcNow;
            if (ttl.TotalHours > 0)
            {
                return $"{(int)ttl.TotalHours}h";
            }
            return null;
        }
    }
    private int CurrentActivityNumber
    {
        get
        {
            var index = GetCurrentActivityIndex();
            return index >= 0 ? index + 1 : 0;
        }
    }
    private int TotalActivities => activities.Count;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Cancel any pending fullscreen attempts
        _autoFullscreenCts?.Cancel();
        _autoFullscreenCts?.Dispose();
        
        if (hubConnection != null)
        {
            try
            {
                await hubConnection.InvokeAsync("Unsubscribe", SessionCode);
                await hubConnection.DisposeAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}