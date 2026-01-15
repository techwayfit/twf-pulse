using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface ISessionService
{
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

    Task<IReadOnlyCollection<Session>> GetSessionsByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default);
}
