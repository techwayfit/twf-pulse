using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
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
}
