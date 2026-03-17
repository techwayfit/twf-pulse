using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Service for broadcasting real-time notifications to participants via SignalR.
/// </summary>
public interface IHubNotificationService
{
    Task PublishSessionStateChangedAsync(Session session, CancellationToken cancellationToken = default);

    Task PublishActivityStateChangedAsync(string sessionCode, Activity activity, CancellationToken cancellationToken = default);

    Task PublishActivityDeletedAsync(string sessionCode, Guid activityId, CancellationToken cancellationToken = default);

    Task PublishResponseReceivedAsync(
        string sessionCode,
        Guid activityId,
        Guid responseId,
        Guid participantId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task PublishDashboardUpdatedAsync(
        string sessionCode,
        Guid? activityId,
        string aggregateType,
        object payload,
        CancellationToken cancellationToken = default);

    Task PublishQuadrantItemAdvancedAsync(
        string sessionCode,
        Guid activityId,
        int itemIndex,
        CancellationToken cancellationToken = default);
}
