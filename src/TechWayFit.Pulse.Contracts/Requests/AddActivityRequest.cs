using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class AddActivityRequest
{
    public int Order { get; set; }

    public ActivityType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public string? Config { get; set; }

    public int? DurationMinutes { get; set; }
}
