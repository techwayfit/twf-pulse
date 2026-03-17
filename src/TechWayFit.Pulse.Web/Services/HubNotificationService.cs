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
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IApiMapper _mapper;
    private readonly TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane.DatabaseBackplaneService? _backplaneService;

    public HubNotificationService(
        IHubContext<WorkshopHub, IWorkshopClient> hub,
        IParticipantService participantService,
        IDateTimeProvider dateTimeProvider,
        IApiMapper mapper,
        IServiceProvider serviceProvider)
    {
        _hub = hub;
        _participantService = participantService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;

        // Try to get backplane service if it's registered
        _backplaneService = serviceProvider.GetService(typeof(TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane.DatabaseBackplaneService))
            as TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane.DatabaseBackplaneService;
    }

    public async Task PublishSessionStateChangedAsync(Session session, CancellationToken cancellationToken = default)
    {
        var participants = await _participantService.GetBySessionAsync(session.Id, cancellationToken);
        var groupName = WorkshopGroupNames.ForSession(session.Code);

        var sessionStateEvent = new SessionStateChangedEvent(
            session.Code,
            _mapper.MapSessionStatus(session.Status),
            session.CurrentActivityId,
            participants.Count,
            _dateTimeProvider.UtcNow);

        await _hub.Clients.Group(groupName).SessionStateChanged(sessionStateEvent);

        // Store in database for other servers if backplane is enabled
        if (_backplaneService != null)
        {
            await _backplaneService.StoreMessageAsync(groupName, nameof(IWorkshopClient.SessionStateChanged), new object[] { sessionStateEvent });
        }
    }

    public async Task PublishActivityStateChangedAsync(string sessionCode, Activity activity, CancellationToken cancellationToken = default)
    {
        var groupName = WorkshopGroupNames.ForSession(sessionCode);
        var activityStateEvent = new ActivityStateChangedEvent(
            sessionCode,
            activity.Id,
            activity.Order,
            activity.Title,
            _mapper.MapActivityStatus(activity.Status),
            activity.OpenedAt,
            activity.ClosedAt,
            _dateTimeProvider.UtcNow);

        await _hub.Clients.Group(groupName).ActivityStateChanged(activityStateEvent);

        if (_backplaneService != null)
        {
            await _backplaneService.StoreMessageAsync(groupName, nameof(IWorkshopClient.ActivityStateChanged), new object[] { activityStateEvent });
        }
    }

    public async Task PublishActivityDeletedAsync(string sessionCode, Guid activityId, CancellationToken cancellationToken = default)
    {
        var groupName = WorkshopGroupNames.ForSession(sessionCode);
        await _hub.Clients.Group(groupName).ActivityDeleted(activityId);

        if (_backplaneService != null)
        {
            await _backplaneService.StoreMessageAsync(groupName, nameof(IWorkshopClient.ActivityDeleted), new object[] { activityId });
        }
    }

    public async Task PublishResponseReceivedAsync(
        string sessionCode,
        Guid activityId,
        Guid responseId,
        Guid participantId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var groupName = WorkshopGroupNames.ForSession(sessionCode);
        var responseEvent = new ResponseReceivedEvent(
            sessionCode,
            activityId,
            responseId,
            participantId,
            createdAt,
            _dateTimeProvider.UtcNow);

        await _hub.Clients.Group(groupName).ResponseReceived(responseEvent);

        if (_backplaneService != null)
        {
            await _backplaneService.StoreMessageAsync(groupName, nameof(IWorkshopClient.ResponseReceived), new object[] { responseEvent });
        }
    }

    public async Task PublishDashboardUpdatedAsync(
        string sessionCode,
        Guid? activityId,
        string aggregateType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var groupName = WorkshopGroupNames.ForSession(sessionCode);
        var dashboardEvent = new DashboardUpdatedEvent(
            sessionCode,
            activityId,
            aggregateType,
            payload,
            _dateTimeProvider.UtcNow);

        await _hub.Clients.Group(groupName).DashboardUpdated(dashboardEvent);

        if (_backplaneService != null)
        {
            await _backplaneService.StoreMessageAsync(groupName, nameof(IWorkshopClient.DashboardUpdated), new object[] { dashboardEvent });
        }
    }

    public async Task PublishQuadrantItemAdvancedAsync(
        string sessionCode,
        Guid activityId,
        int itemIndex,
        CancellationToken cancellationToken = default)
    {
        var groupName = WorkshopGroupNames.ForSession(sessionCode);
        var quadrantEvent = new QuadrantItemAdvancedEvent(
            sessionCode,
            activityId,
            itemIndex,
            _dateTimeProvider.UtcNow);

        await _hub.Clients.Group(groupName).QuadrantItemAdvanced(quadrantEvent);

        if (_backplaneService != null)
        {
            await _backplaneService.StoreMessageAsync(groupName, nameof(IWorkshopClient.QuadrantItemAdvanced), new object[] { quadrantEvent });
        }
    }
}
