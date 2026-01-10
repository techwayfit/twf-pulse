using Microsoft.AspNetCore.SignalR;

namespace TechWayFit.Pulse.Web.Hubs;

public sealed class WorkshopHub : Hub
{
    public Task Subscribe(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            throw new HubException("Session code is required.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, sessionCode.Trim());
    }
}
