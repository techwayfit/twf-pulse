using Microsoft.AspNetCore.SignalR;

namespace TechWayFit.Pulse.Web.Hubs;

/// <summary>
/// SignalR hub for real-time workshop communication
/// </summary>
public sealed class WorkshopHub : Hub<IWorkshopClient>
{
    /// <summary>
    /// Subscribe to session events
    /// </summary>
    public async Task Subscribe(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode))
        {
       throw new ArgumentException("Session code is required", nameof(sessionCode));
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);
    }

    /// <summary>
    /// Unsubscribe from session events  
    /// </summary>
    public async Task Unsubscribe(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode))
        {
      throw new ArgumentException("Session code is required", nameof(sessionCode));
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Client will be automatically removed from all groups
     await base.OnDisconnectedAsync(exception);
    }
}
