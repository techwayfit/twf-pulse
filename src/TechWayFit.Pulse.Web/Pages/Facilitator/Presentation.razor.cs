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
    private bool hasPrevious = false;
    private bool hasNext = false;
    private bool isLoading = true;
    private bool isPerformingAction = false;
    private bool isFullscreen = false;
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
            activities = agenda.Select(ApiMapper.ToAgenda).ToList();
            activity = activities.FirstOrDefault(a => a.Status == Contracts.Enums.ActivityStatus.Open);

            if (activity == null)
            {
                errorMessage = "No active activity found. Please open an activity first.";
                return;
            }

            hasPrevious = activities.Any(a => a.Order < activity.Order);
            hasNext = activities.Any(a => a.Order > activity.Order && a.Status != Contracts.Enums.ActivityStatus.Closed);

            try
            {
                var participants = await ParticipantService.GetBySessionAsync(session.Id);
                participantCount = participants.Count;
            }
            catch
            {
                participantCount = 0;
            }
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

    private async Task MoveNext()
    {
        if (activity == null) return;

        try
        {
            isPerformingAction = true;
            var token = await TokenService.GetFacilitatorTokenAsync(SessionCode);
            if (string.IsNullOrEmpty(token)) return;

            var nextActivity = activities
    .Where(a => a.Order > activity.Order)
  .OrderBy(a => a.Order)
      .FirstOrDefault();

            if (nextActivity == null) return;

            if (session != null)
            {
                await ActivityService.CloseAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
                await ActivityService.OpenAsync(session.Id, nextActivity.ActivityId, DateTimeOffset.UtcNow);
                await SessionService.SetCurrentActivityAsync(session.Id, nextActivity.ActivityId, DateTimeOffset.UtcNow);

                // Broadcast SignalR events
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

                var openedActivity = agenda.FirstOrDefault(a => a.Id == nextActivity.ActivityId);
                if (openedActivity != null)
                {
                    await HubNotifications.PublishActivityStateChangedAsync(SessionCode, openedActivity);
                }
            }

            await LoadPresentation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to move next");
            errorMessage = $"Failed to move to next activity: {ex.Message}";
        }
        finally
        {
            isPerformingAction = false;
        }
    }

    private async Task GoBack()
    {
        if (activity == null) return;

        try
        {
            isPerformingAction = true;
            var token = await TokenService.GetFacilitatorTokenAsync(SessionCode);
            if (string.IsNullOrEmpty(token)) return;

            var previousActivity = activities
              .Where(a => a.Order < activity.Order)
                  .OrderByDescending(a => a.Order)
             .FirstOrDefault();

            if (previousActivity == null) return;

            if (session != null)
            {
                await ActivityService.CloseAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
                await ActivityService.OpenAsync(session.Id, previousActivity.ActivityId, DateTimeOffset.UtcNow);
                await SessionService.SetCurrentActivityAsync(session.Id, previousActivity.ActivityId, DateTimeOffset.UtcNow);

                // Broadcast SignalR events
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

                var openedActivity = agenda.FirstOrDefault(a => a.Id == previousActivity.ActivityId);
                if (openedActivity != null)
                {
                    await HubNotifications.PublishActivityStateChangedAsync(SessionCode, openedActivity);
                }
            }

            await LoadPresentation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to go back");
            errorMessage = $"Failed to go back: {ex.Message}";
        }
        finally
        {
            isPerformingAction = false;
        }
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
    private int CurrentActivityNumber => activity?.Order ?? 0;
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