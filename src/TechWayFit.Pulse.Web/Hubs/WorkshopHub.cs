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
        var groupName = WorkshopGroupNames.ForSession(sessionCode);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Unsubscribe from session events  
    /// </summary>
    public async Task Unsubscribe(string sessionCode)
    {
        var groupName = WorkshopGroupNames.ForSession(sessionCode);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Client will be automatically removed from all groups
     await base.OnDisconnectedAsync(exception);
    }
}
