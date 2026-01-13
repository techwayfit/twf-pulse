using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Domain.Entities;

public sealed class Session
{
    public Session(
        Guid id,
        string code,
        string title,
        string? goal,
        string? context,
        SessionSettings settings,
        JoinFormSchema joinFormSchema,
        SessionStatus status,
        Guid? currentActivityId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset expiresAt,
        Guid? facilitatorUserId = null)
    {
        Id = id;
        Code = code.Trim();
        Title = title.Trim();
        Goal = goal;
        Context = context;
        Settings = settings;
        JoinFormSchema = joinFormSchema;
        Status = status;
        CurrentActivityId = currentActivityId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        ExpiresAt = expiresAt;
        FacilitatorUserId = facilitatorUserId;
    }

    public Guid Id { get; }

    public string Code { get; private set; }

    public string Title { get; private set; }

    public string? Goal { get; private set; }

    public string? Context { get; private set; }

    public SessionSettings Settings { get; private set; }

    public JoinFormSchema JoinFormSchema { get; private set; }

    public SessionStatus Status { get; private set; }

    public Guid? CurrentActivityId { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// ID of the facilitator user who created this session.
    /// Null for legacy sessions created before authentication was implemented.
    /// </summary>
    public Guid? FacilitatorUserId { get; private set; }

    public void SetStatus(SessionStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }

    public void SetCurrentActivity(Guid? activityId, DateTimeOffset updatedAt)
    {
        CurrentActivityId = activityId;
        UpdatedAt = updatedAt;
    }

    public void UpdateJoinFormSchema(JoinFormSchema joinFormSchema, DateTimeOffset updatedAt)
    {
        JoinFormSchema = joinFormSchema;
        UpdatedAt = updatedAt;
    }
}
