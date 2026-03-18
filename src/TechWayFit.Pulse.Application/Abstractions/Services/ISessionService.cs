using TechWayFit.Pulse.Application.Commands;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(
        CreateSessionCommand command,
        CancellationToken cancellationToken = default);

    Task<Session> CreateSessionAsync(
        string code,
        string title,
        string? goal,
        string? context,
        SessionSettings settings,
        JoinFormSchema joinFormSchema,
        DateTimeOffset now,
        Guid? facilitatorUserId = null,
        Guid? groupId = null,
        CancellationToken cancellationToken = default);

    Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Session> UpdateSessionAsync(
        UpdateSessionCommand command,
        CancellationToken cancellationToken = default);

    Task<Session> UpdateSessionAsync(
        Guid sessionId,
        string title,
        string? goal,
        string? context,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<Session> UpdateSessionSettingsAsync(
        Guid sessionId,
        SessionSettings settings,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task SetStatusAsync(Guid sessionId, SessionStatus status, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task SetCurrentActivityAsync(Guid sessionId, Guid? activityId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<Session> UpdateJoinFormSchemaAsync(
        Guid sessionId,
        JoinFormSchema joinFormSchema,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task SetSessionGroupAsync(
        Guid sessionId,
        Guid? groupId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task SetSessionScheduleAsync(
        Guid sessionId,
        DateTime? sessionStart,
        DateTime? sessionEnd,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Session>> GetSessionsByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy a session and its activities to create a new draft session.
    /// </summary>
    /// <param name="sessionId">The ID of the session to copy</param>
    /// <param name="newCode">The code for the new session</param>
    /// <param name="now">Current timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The newly created session copy</returns>
    Task<Session> CopySessionAsync(
        Guid sessionId,
        string newCode,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
