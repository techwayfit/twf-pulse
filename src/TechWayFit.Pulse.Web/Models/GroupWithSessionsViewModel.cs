using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Web.Models;

public class GroupWithSessionsViewModel
{
    public SessionGroup Group { get; set; } = null!;
    public List<SessionSummary> Sessions { get; set; } = new();
    public int TotalSessionCount { get; set; }
}

public class SessionSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsActive { get; set; }
}
