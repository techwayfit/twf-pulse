namespace TechWayFit.Pulse.Domain.Entities;

public sealed class Response
{
    public Response(
        Guid id,
        Guid sessionId,
        Guid activityId,
        Guid participantId,
        string payload,
        IReadOnlyDictionary<string, string?> dimensions,
        DateTimeOffset createdAt)
    {
        Id = id;
        SessionId = sessionId;
        ActivityId = activityId;
        ParticipantId = participantId;
        Payload = payload;
        Dimensions = dimensions;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid SessionId { get; }

    public Guid ActivityId { get; }

    public Guid ParticipantId { get; }

    public string Payload { get; private set; }

    public IReadOnlyDictionary<string, string?> Dimensions { get; private set; }

    public DateTimeOffset CreatedAt { get; }
}
