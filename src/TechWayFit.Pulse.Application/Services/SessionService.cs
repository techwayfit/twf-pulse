using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Application.Services;

public sealed class SessionService : ISessionService
{
    private readonly ISessionRepository _sessions;
    private const int CodeMaxLength = 32;
    private const int TitleMaxLength = 200;

    public SessionService(ISessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<Session> CreateSessionAsync(
        string code,
        string title,
        string? goal,
        string? context,
        SessionSettings settings,
        JoinFormSchema joinFormSchema,
        DateTimeOffset now,
        Guid? facilitatorUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Session code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Session title is required.", nameof(title));
        }

        if (code.Trim().Length > CodeMaxLength)
        {
            throw new ArgumentException($"Session code must be <= {CodeMaxLength} characters.", nameof(code));
        }

        if (title.Trim().Length > TitleMaxLength)
        {
            throw new ArgumentException($"Session title must be <= {TitleMaxLength} characters.", nameof(title));
        }

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(joinFormSchema);

        var existing = await _sessions.GetByCodeAsync(code.Trim(), cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Session code already exists.");
        }

        var session = new Session(
            Guid.NewGuid(),
            code.Trim(),
            title.Trim(),
            goal,
            context,
            settings,
            joinFormSchema,
            SessionStatus.Draft,
            null,
            now,
            now,
            now.AddMinutes(settings.TtlMinutes),
            facilitatorUserId);

        await _sessions.AddAsync(session, cancellationToken);
        return session;
    }

    public Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _sessions.GetByCodeAsync(code, cancellationToken);
    }

    public async Task SetStatusAsync(
        Guid sessionId,
        SessionStatus status,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        session.SetStatus(status, now);
        await _sessions.UpdateAsync(session, cancellationToken);
    }

    public async Task SetCurrentActivityAsync(
        Guid sessionId,
        Guid? activityId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        session.SetCurrentActivity(activityId, now);
        await _sessions.UpdateAsync(session, cancellationToken);
    }

    public async Task<Session> UpdateJoinFormSchemaAsync(
        Guid sessionId,
        JoinFormSchema joinFormSchema,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(joinFormSchema);

        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        session.UpdateJoinFormSchema(joinFormSchema, now);
        await _sessions.UpdateAsync(session, cancellationToken);
        return session;
    }
}
