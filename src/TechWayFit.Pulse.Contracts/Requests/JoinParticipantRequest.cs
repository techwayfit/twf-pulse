namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class JoinParticipantRequest
{
    public string? DisplayName { get; set; }

    public bool IsAnonymous { get; set; }

    public Dictionary<string, string?> Dimensions { get; set; } = new();
}
