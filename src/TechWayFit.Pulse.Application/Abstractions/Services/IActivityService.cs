using TechWayFit.Pulse.Application.Abstractions.Results;
using TechWayFit.Pulse.Application.Commands;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IActivityService
{
    Task<Result<Activity>> AddActivityAsync(
        AddActivityCommand command,
        CancellationToken cancellationToken = default);

    Task<Activity> AddActivityAsync(
        Guid sessionId,
        int order,
        ActivityType type,
        string title,
        string? prompt,
        string? config,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Activity>> GetAgendaAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<Result<Activity>> UpdateActivityAsync(
        UpdateActivityCommand command,
        CancellationToken cancellationToken = default);

    Task<Activity> UpdateActivityAsync(
        Guid sessionId,
        Guid activityId,
        string title,
        string? prompt,
        string? config,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteActivityAsync(
        DeleteActivityCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteActivityAsync(
        Guid sessionId,
        Guid activityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy an existing activity to create a new pending activity with " (Copy)" appended to the title.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="activityId">The activity ID to copy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The newly created activity copy</returns>
    Task<Result<Activity>> CopyActivityAsync(
        CopyActivityCommand command,
        CancellationToken cancellationToken = default);

    Task<Activity> CopyActivityAsync(
        Guid sessionId,
        Guid activityId,
        CancellationToken cancellationToken = default);

    Task<Result> OpenAsync(
        OpenActivityCommand command,
        CancellationToken cancellationToken = default);

    Task OpenAsync(Guid sessionId, Guid activityId, DateTimeOffset openedAt, CancellationToken cancellationToken = default);

    Task<Result> ReopenAsync(
        ReopenActivityCommand command,
        CancellationToken cancellationToken = default);

    Task ReopenAsync(Guid sessionId, Guid activityId, DateTimeOffset openedAt, CancellationToken cancellationToken = default);

    Task<Result> CloseAsync(
        CloseActivityCommand command,
        CancellationToken cancellationToken = default);

    Task CloseAsync(Guid sessionId, Guid activityId, DateTimeOffset closedAt, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Activity>>> ReorderAsync(
        ReorderActivitiesCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Activity>> ReorderAsync(
        Guid sessionId,
        IReadOnlyList<Guid> orderedActivityIds,
        CancellationToken cancellationToken = default);
}
