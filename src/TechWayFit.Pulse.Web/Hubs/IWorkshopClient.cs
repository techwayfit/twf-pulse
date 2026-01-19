using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.Web.Hubs;

/// <summary>
/// Typed SignalR client interface for workshop real-time events
/// </summary>
public interface IWorkshopClient
{
    /// <summary>
    /// Session state has changed (Draft ? Live ? Ended)
    /// </summary>
    Task SessionStateChanged(SessionStateChangedEvent evt);

    /// <summary>
    /// Activity has been opened or closed
    /// </summary>
    Task ActivityStateChanged(ActivityStateChangedEvent evt);

    /// <summary>
    /// New participant has joined the session
    /// </summary>
    Task ParticipantJoined(ParticipantJoinedEvent evt);

    /// <summary>
    /// New response has been submitted to an activity
    /// </summary>
    Task ResponseReceived(ResponseReceivedEvent evt);

    /// <summary>
    /// Dashboard data has been updated with new aggregations
    /// </summary>
    Task DashboardUpdated(DashboardUpdatedEvent evt);

    /// <summary>
    /// Activity has been deleted
    /// </summary>
    Task ActivityDeleted(Guid activityId);
}

/// <summary>
/// Event data for session state changes
/// </summary>
public sealed record SessionStateChangedEvent(
    string SessionCode,
    SessionStatus Status,
    Guid? CurrentActivityId,
    int ParticipantCount,
    DateTimeOffset Timestamp);

/// <summary>
/// Event data for activity state changes
/// </summary>
public sealed record ActivityStateChangedEvent(
 string SessionCode,
    Guid ActivityId,
    int Order,
    string Title,
    TechWayFit.Pulse.Contracts.Enums.ActivityStatus Status,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset Timestamp);

/// <summary>
/// Event data for new participant joins
/// </summary>
public sealed record ParticipantJoinedEvent(
    string SessionCode,
    Guid ParticipantId,
    string? DisplayName,
    int TotalParticipantCount,
    DateTimeOffset Timestamp);

/// <summary>
/// Event data for new responses
/// </summary>
public sealed record ResponseReceivedEvent(
    string SessionCode,
  Guid ActivityId,
    Guid ResponseId,
    Guid ParticipantId,
    DateTimeOffset CreatedAt,
    DateTimeOffset Timestamp);

/// <summary>
/// Event data for dashboard updates
/// </summary>
public sealed record DashboardUpdatedEvent(
    string SessionCode,
 Guid? ActivityId,
    string AggregateType,
    object Payload,
    DateTimeOffset Timestamp);