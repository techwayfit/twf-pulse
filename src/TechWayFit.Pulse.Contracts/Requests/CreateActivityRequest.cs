using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.Contracts.Requests;

public sealed record CreateActivityRequest(
    ActivityType Type,
    int Order,
    string Title,
    string? Prompt,
    string Config,
    int? DurationMinutes = null
);
