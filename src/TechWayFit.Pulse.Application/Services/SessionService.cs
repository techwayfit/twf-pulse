using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Results;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Commands;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Application.Services;

public sealed class SessionService : ISessionService
{
    private readonly ISessionRepository _sessions;
    private readonly IActivityRepository _activities;
    private const int CodeMaxLength = 32;
    private const int TitleMaxLength = 200;

    public SessionService(ISessionRepository sessions, IActivityRepository activities)
    {
        _sessions = sessions;
        _activities = activities;
    }

    public async Task<Result<Session>> CreateSessionAsync(
        CreateSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var session = await CreateSessionAsync(
                command.Code,
                command.Title,
                command.Goal,
                command.Context,
                command.Settings,
                command.JoinFormSchema,
                command.Now,
                command.FacilitatorUserId,
                command.GroupId,
                cancellationToken);
            return Result<Session>.Success(session);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Session>.Failure(MapError(ex));
        }
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
        Guid? groupId = null,
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
            now.AddDays(30), // Initial expiry far in future - actual TTL starts when session goes Live
            facilitatorUserId,
            groupId);

        await _sessions.AddAsync(session, cancellationToken);
        return session;
    }

    public Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _sessions.GetByCodeAsync(code, cancellationToken);
    }

    public async Task<Result<Session>> UpdateSessionAsync(
        UpdateSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var session = await UpdateSessionAsync(
                command.SessionId,
                command.Title,
                command.Goal,
                command.Context,
                command.Now,
                cancellationToken);
            return Result<Session>.Success(session);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Session>.Failure(MapError(ex));
        }
    }

    public async Task<Result> SetStatusAsync(
        SetSessionStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await SetStatusAsync(command.SessionId, command.Status, command.Now, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    public async Task<Result> SetCurrentActivityAsync(
        SetCurrentActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await SetCurrentActivityAsync(command.SessionId, command.ActivityId, command.Now, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    public async Task<Result<Session>> UpdateJoinFormSchemaAsync(
        UpdateJoinFormSchemaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var session = await UpdateJoinFormSchemaAsync(command.SessionId, command.JoinFormSchema, command.Now, cancellationToken);
            return Result<Session>.Success(session);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Session>.Failure(MapError(ex));
        }
    }

    public async Task<Result> SetSessionGroupAsync(
        SetSessionGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await SetSessionGroupAsync(command.SessionId, command.GroupId, command.Now, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    public async Task<Result> SetSessionScheduleAsync(
        SetSessionScheduleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await SetSessionScheduleAsync(command.SessionId, command.SessionStart, command.SessionEnd, command.Now, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    public async Task<Result<Session>> CopySessionAsync(
        CopySessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var session = await CopySessionAsync(command.SessionId, command.NewCode, command.Now, cancellationToken);
            return Result<Session>.Success(session);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Session>.Failure(MapError(ex));
        }
    }

    public async Task<Session> UpdateSessionAsync(
        Guid sessionId,
        string title,
        string? goal,
        string? context,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Session title is required.", nameof(title));
        }

        if (title.Trim().Length > TitleMaxLength)
        {
            throw new ArgumentException($"Session title must be <= {TitleMaxLength} characters.", nameof(title));
        }

        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        session.Update(title, goal, context, now);
        await _sessions.UpdateAsync(session, cancellationToken);
        return session;
    }

    public async Task<Session> UpdateSessionSettingsAsync(
        Guid sessionId,
        SessionSettings settings,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        session.UpdateSettings(settings, now);
        await _sessions.UpdateAsync(session, cancellationToken);
        return session;
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
        
        // When session goes Live, set the actual expiry time based on TTL
        if (status == SessionStatus.Live)
        {
            session.SetExpiresAt(now.AddMinutes(session.Settings.TtlMinutes), now);
        }
        
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

    public async Task SetSessionGroupAsync(
        Guid sessionId,
        Guid? groupId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        session.SetGroup(groupId, now);
        await _sessions.UpdateAsync(session, cancellationToken);
    }

    public async Task SetSessionScheduleAsync(
        Guid sessionId,
        DateTime? sessionStart,
        DateTime? sessionEnd,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        session.SetSessionSchedule(sessionStart, sessionEnd, now);
        await _sessions.UpdateAsync(session, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Session>> GetSessionsByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        return await _sessions.GetByGroupAsync(groupId, facilitatorUserId, cancellationToken);
    }

    public async Task<Session> CopySessionAsync(
        Guid sessionId,
        string newCode,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        // Get the original session
        var originalSession = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (originalSession is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        // Verify the new code is unique
        if (string.IsNullOrWhiteSpace(newCode))
        {
            throw new ArgumentException("Session code is required.", nameof(newCode));
        }

        if (newCode.Trim().Length > CodeMaxLength)
        {
            throw new ArgumentException($"Session code must be <= {CodeMaxLength} characters.", nameof(newCode));
        }

        var existing = await _sessions.GetByCodeAsync(newCode.Trim(), cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Session code already exists.");
        }

        // Create new title with timestamp - format: "Copy-DDMMYYHHmmss"
        var timestamp = now.ToString("ddMMyyHHmmss");
        var newTitle = $"{originalSession.Title} - Copy-{timestamp}";
        
        // Ensure title doesn't exceed max length
        if (newTitle.Length > TitleMaxLength)
        {
            var maxOriginalTitleLength = TitleMaxLength - timestamp.Length - 9; // 9 chars for " - Copy-"
            newTitle = $"{originalSession.Title.Substring(0, maxOriginalTitleLength)} - Copy-{timestamp}";
        }

        // Create the new session in Draft state
        var newSession = new Session(
            Guid.NewGuid(),
            newCode.Trim(),
            newTitle,
            originalSession.Goal,
            originalSession.Context,
            originalSession.Settings, // Copy settings as-is
            originalSession.JoinFormSchema, // Copy join form schema
            SessionStatus.Draft, // Always start as Draft
            null, // No current activity
            now,
            now,
            now.AddDays(30), // Initial expiry
            originalSession.FacilitatorUserId,
            originalSession.GroupId, // Keep same group
            null, // Clear session start
            null); // Clear session end

        await _sessions.AddAsync(newSession, cancellationToken);

        // Copy all activities from the original session
        var originalActivities = await _activities.GetBySessionAsync(originalSession.Id, cancellationToken);
        
        foreach (var originalActivity in originalActivities.OrderBy(a => a.Order))
        {
            // Create new activity without session-specific data
            var newActivity = new Activity(
                Guid.NewGuid(),
                newSession.Id,
                originalActivity.Order,
                originalActivity.Type,
                originalActivity.Title,
                originalActivity.Prompt,
                originalActivity.Config, // Copy config as-is
                ActivityStatus.Pending, // Reset to Pending (not Open/Closed)
                null, // Clear OpenedAt
                null, // Clear ClosedAt
                originalActivity.DurationMinutes);

            await _activities.AddAsync(newActivity, cancellationToken);
        }

        return newSession;
    }

    private static Error MapError(Exception ex)
    {
        return ex switch
        {
            ArgumentException argumentException => ResultErrors.Validation(argumentException.Message),
            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => new Error("not_found", invalidOperationException.Message, ErrorType.NotFound),
            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                => new Error("conflict", invalidOperationException.Message, ErrorType.Conflict),
            InvalidOperationException invalidOperationException => ResultErrors.Validation(invalidOperationException.Message),
            _ => ResultErrors.Unexpected("An unexpected error occurred.")
        };
    }
}
