using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Web.Services;

/// <summary>
/// Service for broadcasting real-time notifications to participants via SignalR
/// </summary>
public interface IHubNotificationService
{
    /// <summary>
    /// Broadcast session state change event to all session participants
    /// </summary>
    Task PublishSessionStateChangedAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast activity state change event to all session participants
    /// </summary>
    Task PublishActivityStateChangedAsync(string sessionCode, Activity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast activity deleted event to all session participants
    /// </summary>
    Task PublishActivityDeletedAsync(string sessionCode, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast response received event to all session participants
    /// </summary>
    Task PublishResponseReceivedAsync(
        string sessionCode,
        Guid activityId,
        Guid responseId,
        Guid participantId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast dashboard updated event to all session participants
    /// </summary>
    Task PublishDashboardUpdatedAsync(
        string sessionCode,
        Guid? activityId,
        string aggregateType,
        object payload,
        CancellationToken cancellationToken = default);
}
