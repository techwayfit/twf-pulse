using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Results;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Commands;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Services;

public sealed class ActivityService : IActivityService
{
    private readonly IActivityRepository _activities;
    private readonly ISessionRepository _sessions;
    private const int TitleMaxLength = 200;
    private const int PromptMaxLength = 1000;

    public ActivityService(IActivityRepository activities, ISessionRepository sessions)
    {
        _activities = activities;
        _sessions = sessions;
    }

    public async Task<Result<Activity>> AddActivityAsync(
        AddActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var activity = await AddActivityAsync(
                command.SessionId,
                command.Order,
                command.Type,
                command.Title,
                command.Prompt,
                command.Config,
                command.DurationMinutes,
                cancellationToken);
            return Result<Activity>.Success(activity);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Activity>.Failure(MapError(ex));
        }
    }

    public async Task<Activity> AddActivityAsync(
        Guid sessionId,
        int order,
        ActivityType type,
        string title,
        string? prompt,
        string? config,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Activity title is required.", nameof(title));
        }

        if (title.Trim().Length > TitleMaxLength)
        {
            throw new ArgumentException($"Activity title must be <= {TitleMaxLength} characters.", nameof(title));
        }

        if (!string.IsNullOrWhiteSpace(prompt) && prompt.Trim().Length > PromptMaxLength)
        {
            throw new ArgumentException($"Prompt must be <= {PromptMaxLength} characters.", nameof(prompt));
        }

        if (durationMinutes.HasValue && durationMinutes.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be greater than zero.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        var activity = new Activity(
            Guid.NewGuid(),
            sessionId,
            order,
            type,
            title.Trim(),
            prompt,
            config,
            ActivityStatus.Pending,
            null,
            null,
            durationMinutes);

        await _activities.AddAsync(activity, cancellationToken);
        return activity;
    }

    public Task<IReadOnlyList<Activity>> GetAgendaAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _activities.GetBySessionAsync(sessionId, cancellationToken);
    }

    public async Task<Result<Activity>> UpdateActivityAsync(
        UpdateActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var activity = await UpdateActivityAsync(
                command.SessionId,
                command.ActivityId,
                command.Title,
                command.Prompt,
                command.Config,
                command.DurationMinutes,
                cancellationToken);
            return Result<Activity>.Success(activity);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Activity>.Failure(MapError(ex));
        }
    }

    public async Task<Activity> UpdateActivityAsync(
        Guid sessionId,
        Guid activityId,
        string title,
        string? prompt,
        string? config,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Activity title is required.", nameof(title));
        }

        if (title.Trim().Length > TitleMaxLength)
        {
            throw new ArgumentException($"Activity title must be <= {TitleMaxLength} characters.", nameof(title));
        }

        if (!string.IsNullOrWhiteSpace(prompt) && prompt.Trim().Length > PromptMaxLength)
        {
            throw new ArgumentException($"Prompt must be <= {PromptMaxLength} characters.", nameof(prompt));
        }

        if (durationMinutes.HasValue && durationMinutes.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be greater than zero.");
        }

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        if (activity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity does not belong to the session.");
        }

        activity.Update(title, prompt, config, durationMinutes);
        await _activities.UpdateAsync(activity, cancellationToken);
        return activity;
    }

    public async Task DeleteActivityAsync(
        Guid sessionId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        if (activity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity does not belong to the session.");
        }

        if (activity.Status != ActivityStatus.Pending)
        {
            throw new InvalidOperationException("Only pending activities can be deleted.");
        }

        await _activities.DeleteAsync(activity, cancellationToken);
    }

    public async Task<Result> DeleteActivityAsync(
        DeleteActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await DeleteActivityAsync(command.SessionId, command.ActivityId, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    public async Task<Result<Activity>> CopyActivityAsync(
        CopyActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var activity = await CopyActivityAsync(command.SessionId, command.ActivityId, cancellationToken);
            return Result<Activity>.Success(activity);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Activity>.Failure(MapError(ex));
        }
    }

    public async Task<Activity> CopyActivityAsync(
        Guid sessionId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        var sourceActivity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (sourceActivity is null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        if (sourceActivity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity does not belong to the session.");
        }

        // Get all activities in the session to determine the new order
        var allActivities = await _activities.GetBySessionAsync(sessionId, cancellationToken);
        var newOrder = allActivities.Count + 1;

        // Create copy with " (Copy)" appended to title
        var copiedTitle = sourceActivity.Title;
        const string copySuffix = " (Copy)";

        // Ensure the title doesn't exceed max length after adding suffix
        if (copiedTitle.Length + copySuffix.Length > TitleMaxLength)
        {
            copiedTitle = copiedTitle.Substring(0, TitleMaxLength - copySuffix.Length);
        }
        copiedTitle += copySuffix;

        var copiedActivity = new Activity(
            Guid.NewGuid(),
            sessionId,
            newOrder,
            sourceActivity.Type,
            copiedTitle,
            sourceActivity.Prompt,
            sourceActivity.Config,
            ActivityStatus.Pending, // Always create as Pending
            null, // Not opened
            null, // Not closed
            sourceActivity.DurationMinutes);

        await _activities.AddAsync(copiedActivity, cancellationToken);
        return copiedActivity;
    }

    public async Task<IReadOnlyList<Activity>> ReorderAsync(
        Guid sessionId,
        IReadOnlyList<Guid> orderedActivityIds,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(orderedActivityIds);

        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        var activities = await _activities.GetBySessionAsync(sessionId, cancellationToken);
        if (activities.Count == 0)
        {
            return Array.Empty<Activity>();
        }

        if (orderedActivityIds.Count != activities.Count)
        {
            throw new InvalidOperationException("Reorder list must include all session activities.");
        }

        var activityLookup = activities.ToDictionary(activity => activity.Id);
        var used = new HashSet<Guid>();

        for (var index = 0; index < orderedActivityIds.Count; index++)
        {
            var activityId = orderedActivityIds[index];
            if (!activityLookup.TryGetValue(activityId, out var activity))
            {
                throw new InvalidOperationException("Activity not found in session.");
            }

            if (!used.Add(activityId))
            {
                throw new InvalidOperationException("Activity list contains duplicates.");
            }

            activity.UpdateOrder(index + 1);
            await _activities.UpdateAsync(activity, cancellationToken);
        }

        return await _activities.GetBySessionAsync(sessionId, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Activity>>> ReorderAsync(
        ReorderActivitiesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var activities = await ReorderAsync(command.SessionId, command.OrderedActivityIds, cancellationToken);
            return Result<IReadOnlyList<Activity>>.Success(activities);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<IReadOnlyList<Activity>>.Failure(MapError(ex));
        }
    }

    public async Task<Result> OpenAsync(
        OpenActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await OpenAsync(command.SessionId, command.ActivityId, command.OpenedAt, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    public async Task OpenAsync(
        Guid sessionId,
        Guid activityId,
        DateTimeOffset openedAt,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        if (activity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity does not belong to the session.");
        }

        if (activity.Status == ActivityStatus.Closed)
        {
            throw new InvalidOperationException("Closed activities cannot be re-opened.");
        }

        if (activity.Status == ActivityStatus.Open)
        {
            return;
        }

        activity.Open(openedAt);
        await _activities.UpdateAsync(activity, cancellationToken);
    }

    public async Task ReopenAsync(
        Guid sessionId,
        Guid activityId,
        DateTimeOffset openedAt,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        if (activity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity does not belong to the session.");
        }

        if (activity.Status != ActivityStatus.Closed)
        {
            throw new InvalidOperationException("Only closed activities can be reopened.");
        }

        activity.Open(openedAt);
        await _activities.UpdateAsync(activity, cancellationToken);
    }

    public async Task<Result> ReopenAsync(
        ReopenActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await ReopenAsync(command.SessionId, command.ActivityId, command.OpenedAt, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    public async Task CloseAsync(
        Guid sessionId,
        Guid activityId,
        DateTimeOffset closedAt,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        if (activity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity does not belong to the session.");
        }

        if (activity.Status != ActivityStatus.Open)
        {
            throw new InvalidOperationException("Only open activities can be closed.");
        }

        activity.Close(closedAt);
        await _activities.UpdateAsync(activity, cancellationToken);
    }

    public async Task<Result> CloseAsync(
        CloseActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            await CloseAsync(command.SessionId, command.ActivityId, command.ClosedAt, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(MapError(ex));
        }
    }

    private static Error MapError(Exception ex)
    {
        return ex switch
        {
            ArgumentException argumentException => ResultErrors.Validation(argumentException.Message),
            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => new Error("not_found", invalidOperationException.Message, ErrorType.NotFound),
            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("does not belong", StringComparison.OrdinalIgnoreCase)
                => new Error("forbidden", invalidOperationException.Message, ErrorType.Forbidden),
            InvalidOperationException invalidOperationException => ResultErrors.Validation(invalidOperationException.Message),
            _ => ResultErrors.Unexpected("An unexpected error occurred.")
        };
    }
}
