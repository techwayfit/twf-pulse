namespace TechWayFit.Pulse.Domain.Entities;

public sealed class Participant
{
    public Participant(
        Guid id,
        Guid sessionId,
        string? displayName,
        bool isAnonymous,
        IReadOnlyDictionary<string, string?> dimensions,
        DateTimeOffset joinedAt)
    {
        Id = id;
        SessionId = sessionId;
        DisplayName = displayName;
        IsAnonymous = isAnonymous;
        Dimensions = dimensions;
        JoinedAt = joinedAt;
    }

    public Guid Id { get; }

    public Guid SessionId { get; }

    public string? DisplayName { get; private set; }

    public bool IsAnonymous { get; private set; }

    public IReadOnlyDictionary<string, string?> Dimensions { get; private set; }

    public DateTimeOffset JoinedAt { get; }
}
