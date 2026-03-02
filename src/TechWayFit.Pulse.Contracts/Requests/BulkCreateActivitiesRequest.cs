using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.Contracts.Requests;

public sealed record BulkCreateActivitiesRequest(
IReadOnlyList<BulkActivityItem> Activities
);

public sealed record BulkActivityItem(
    int Order,
    ActivityType Type,
    string Title,
    string? Prompt,
    string Config,
 int? DurationMinutes = null
);
