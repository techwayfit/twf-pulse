using Microsoft.AspNetCore.SignalR;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Hubs;

namespace TechWayFit.Pulse.Web.Services;

/// <summary>
/// Service for broadcasting real-time notifications to participants via SignalR
/// </summary>
public sealed class HubNotificationService : IHubNotificationService
{
    private readonly IHubContext<WorkshopHub, IWorkshopClient> _hub;
    private readonly IParticipantService _participantService;

    public HubNotificationService(
        IHubContext<WorkshopHub, IWorkshopClient> hub,
        IParticipantService participantService)
    {
        _hub = hub;
        _participantService = participantService;
    }

    public async Task PublishSessionStateChangedAsync(Session session, CancellationToken cancellationToken = default)
    {
        var participants = await _participantService.GetBySessionAsync(session.Id, cancellationToken);

        var sessionStateEvent = new SessionStateChangedEvent(
            session.Code,
            ApiMapper.MapSessionStatus(session.Status),
            session.CurrentActivityId,
            participants.Count,
            DateTimeOffset.UtcNow);

        await _hub.Clients.Group(session.Code).SessionStateChanged(sessionStateEvent);
    }

    public async Task PublishActivityStateChangedAsync(string sessionCode, Activity activity, CancellationToken cancellationToken = default)
    {
        var activityStateEvent = new ActivityStateChangedEvent(
            sessionCode,
            activity.Id,
            activity.Order,
            activity.Title,
            ApiMapper.MapActivityStatus(activity.Status),
            activity.OpenedAt,
            activity.ClosedAt,
            DateTimeOffset.UtcNow);

        await _hub.Clients.Group(sessionCode).ActivityStateChanged(activityStateEvent);
    }

    public async Task PublishActivityDeletedAsync(string sessionCode, Guid activityId, CancellationToken cancellationToken = default)
    {
        await _hub.Clients.Group(sessionCode).ActivityDeleted(activityId);
    }
}
