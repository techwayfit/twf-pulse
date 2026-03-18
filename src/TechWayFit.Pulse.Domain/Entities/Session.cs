using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Events;
using TechWayFit.Pulse.Domain.Exceptions;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Domain.Entities;

public sealed class Session
{
    private readonly List<IDomainEvent> _domainEvents = new();

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
        Guid? facilitatorUserId = null,
        Guid? groupId = null,
        DateTime? sessionStart = null,
        DateTime? sessionEnd = null)
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
        GroupId = groupId;
        SessionStart = sessionStart;
        SessionEnd = sessionEnd;
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

    /// <summary>
    /// ID of the group this session belongs to.
    /// Null if session is not assigned to any group.
    /// </summary>
    public Guid? GroupId { get; private set; }

    /// <summary>
    /// Planned start date/time for the workshop session.
    /// Used for planning purposes only, does not auto-start the session.
    /// </summary>
    public DateTime? SessionStart { get; private set; }

    /// <summary>
    /// Planned end date/time for the workshop session.
    /// Used for planning purposes only, does not auto-end the session.
    /// </summary>
    public DateTime? SessionEnd { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        if (_domainEvents.Count == 0)
        {
            return Array.Empty<IDomainEvent>();
        }

        var drained = _domainEvents.ToArray();
        _domainEvents.Clear();
        return drained;
    }

    public void SetStatus(SessionStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }

    public void SetExpiresAt(DateTimeOffset expiresAt, DateTimeOffset updatedAt)
    {
        ExpiresAt = expiresAt;
        UpdatedAt = updatedAt;
    }

    public void SetCurrentActivity(Guid? activityId, DateTimeOffset updatedAt)
    {
        CurrentActivityId = activityId;
        UpdatedAt = updatedAt;
    }

    public void Update(string title, string? goal, string? context, DateTimeOffset updatedAt)
    {
        Title = title.Trim();
        Goal = goal;
        Context = context;
        UpdatedAt = updatedAt;
    }

    public void UpdateSettings(SessionSettings settings, DateTimeOffset updatedAt)
    {
        Settings = settings;
        UpdatedAt = updatedAt;
    }

    public void UpdateJoinFormSchema(JoinFormSchema joinFormSchema, DateTimeOffset updatedAt)
    {
        JoinFormSchema = joinFormSchema;
        UpdatedAt = updatedAt;
    }

    public void SetGroup(Guid? groupId, DateTimeOffset updatedAt)
    {
        GroupId = groupId;
        UpdatedAt = updatedAt;
    }

    public void SetSessionSchedule(DateTime? sessionStart, DateTime? sessionEnd, DateTimeOffset updatedAt)
    {
        SessionStart = sessionStart;
        SessionEnd = sessionEnd;
        UpdatedAt = updatedAt;
    }

    public Response SubmitResponse(
        Activity activity,
        Participant participant,
        string payload,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(participant);

        if (Status != SessionStatus.Live)
        {
            throw new DomainRuleViolationException("Session is not live.");
        }

        if (activity.SessionId != Id)
        {
            throw new DomainRuleViolationException("Activity not found for this session.");
        }

        if (Settings.StrictCurrentActivityOnly && CurrentActivityId != activity.Id)
        {
            throw new DomainRuleViolationException("Responses are locked to the current activity.");
        }

        if (activity.Status != ActivityStatus.Open)
        {
            throw new DomainRuleViolationException("Activity is not open.");
        }

        if (participant.SessionId != Id)
        {
            throw new DomainRuleViolationException("Participant not found for this session.");
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainRuleViolationException("Response payload is required.");
        }

        var response = new Response(
            Guid.NewGuid(),
            Id,
            activity.Id,
            participant.Id,
            payload.Trim(),
            participant.Dimensions,
            createdAt);

        _domainEvents.Add(new ResponseSubmittedDomainEvent(
            SessionId: Id,
            ActivityId: activity.Id,
            ParticipantId: participant.Id,
            ResponseId: response.Id,
            OccurredAt: createdAt));

        return response;
    }
}
