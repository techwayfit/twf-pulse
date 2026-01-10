namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class SubmitResponseRequest
{
    public Guid ParticipantId { get; set; }

    public string Payload { get; set; } = string.Empty;
}
