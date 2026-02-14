using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Hubs;
using TechWayFit.Pulse.Web.Services;

namespace TechWayFit.Pulse.Web.Pages.Facilitator;

public partial class Presentation : IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private IActivityService ActivityService { get; set; } = default!;
    [Inject] private IParticipantService ParticipantService { get; set; } = default!;
    [Inject] private IClientTokenService TokenService { get; set; } = default!;
    [Inject] private ILogger<Presentation> Logger { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Code { get; set; }

    private string SessionCode => Code ?? string.Empty;

    private AgendaActivityResponse? activity;
    private List<AgendaActivityResponse> activities = new();
    private int participantCount = 0;
    private int responseCount = 0;
    private bool hasPrevious = false;
    private bool hasNext = false;
    private bool isLoading = true;
    private bool isPerformingAction = false;
    private string errorMessage = string.Empty;
    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await LoadPresentation();
        await SetupSignalR();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Enter fullscreen mode automatically
                await JS.InvokeVoidAsync("eval", @"
          // Request fullscreen
         const elem = document.documentElement;
         if (elem.requestFullscreen) {
   elem.requestFullscreen().catch(err => {
    console.log('Fullscreen request denied:', err);
              });
     } else if (elem.webkitRequestFullscreen) {
     elem.webkitRequestFullscreen();
          } else if (elem.msRequestFullscreen) {
          elem.msRequestFullscreen();
    }
          ");

                await JS.InvokeVoidAsync("eval", @"
     let timerInterval = null;

       function startPresenterTimer() {
           const timerEl = document.getElementById('activity-timer-presenter');
      if (!timerEl) return;

     const durationMinutes = parseInt(timerEl.dataset.duration);
      const openedAt = new Date(timerEl.dataset.openedAt);
    const timerText = timerEl.querySelector('.timer-text');

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

          if (remainingSeconds === 0) {
   timerEl.classList.add('timer-expired');
        timerText.textContent = 'Time\\'s Up!';
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

            startPresenterTimer();

       // ESC key to exit fullscreen and return to live view
               document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
         e.preventDefault(); // Prevent default ESC behavior
     
 // Exit fullscreen first
  if (document.fullscreenElement || document.webkitFullscreenElement || document.msFullscreenElement) {
          if (document.exitFullscreen) {
    document.exitFullscreen();
          } else if (document.webkitExitFullscreen) {
      document.webkitExitFullscreen();
    } else if (document.msExitFullscreen) {
        document.msExitFullscreen();
       }
        }
       
       // Navigate after a short delay to allow fullscreen to exit
setTimeout(() => {
window.location.href = '/facilitator/live?code=' + '" + SessionCode + @"';
         }, 200);
  }
           });
  ");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize presenter mode");
            }
        }
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

            var session = await SessionService.GetByCodeAsync(SessionCode);
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

            var session = await SessionService.GetByCodeAsync(SessionCode);
            if (session != null)
            {
                await ActivityService.CloseAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
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

            var session = await SessionService.GetByCodeAsync(SessionCode);
            if (session != null)
            {
                await ActivityService.CloseAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
                await ActivityService.OpenAsync(session.Id, nextActivity.ActivityId, DateTimeOffset.UtcNow);
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

            var session = await SessionService.GetByCodeAsync(SessionCode);
            if (session != null)
            {
                await ActivityService.CloseAsync(session.Id, activity.ActivityId, DateTimeOffset.UtcNow);
                await ActivityService.OpenAsync(session.Id, previousActivity.ActivityId, DateTimeOffset.UtcNow);
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
            await JS.InvokeVoidAsync("eval", @"
        if (document.exitFullscreen) {
    document.exitFullscreen();
                } else if (document.webkitExitFullscreen) {
       document.webkitExitFullscreen();
} else if (document.msExitFullscreen) {
      document.msExitFullscreen();
    }
    ");

            // Wait a bit for fullscreen to exit
            await Task.Delay(200);

            // Navigate back to live view
            Navigation.NavigateTo($"/facilitator/live?code={SessionCode}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to exit presenter mode");
            // Still try to navigate even if fullscreen exit fails
            Navigation.NavigateTo($"/facilitator/live?code={SessionCode}");
        }
    }

    public async ValueTask DisposeAsync()
    {
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
