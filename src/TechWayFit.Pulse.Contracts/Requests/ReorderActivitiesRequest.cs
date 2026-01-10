namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class ReorderActivitiesRequest
{
    public List<Guid> ActivityIds { get; set; } = new();
}
