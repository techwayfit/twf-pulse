namespace TechWayFit.Pulse.Web.Models;

public sealed class SessionReportViewModel
{
    public Guid SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public string SessionTitle { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public string? Context { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public int ParticipantCount { get; set; }
    public int TotalResponses { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public IReadOnlyList<ParticipantFieldColumn> ParticipantColumns { get; set; } = [];
    public IReadOnlyList<ParticipantReportItem> Participants { get; set; } = [];
    public IReadOnlyList<ActivityReportItem> Activities { get; set; } = [];
}

public sealed class ParticipantFieldColumn
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class ParticipantReportItem
{
    public Guid ParticipantId { get; set; }
    public string DisplayName { get; set; } = "Anonymous";
    public DateTimeOffset JoinedAt { get; set; }
    public int ResponseCount { get; set; }
    public IReadOnlyDictionary<string, string?> Dimensions { get; set; } = new Dictionary<string, string?>();
}

public sealed class ActivityReportItem
{
    public Guid ActivityId { get; set; }
    public int Order { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Prompt { get; set; }
    public int ResponseCount { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public ActivityChartModel Chart { get; set; } = new();
    public IReadOnlyList<ActivityResponseItem> Responses { get; set; } = [];
}

public sealed class ActivityResponseItem
{
    public Guid ResponseId { get; set; }
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "Anonymous";
    public DateTimeOffset CreatedAt { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public sealed class ActivityChartModel
{
    public string Type { get; set; } = "bar";
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Labels { get; set; } = [];
    public IReadOnlyList<double> Values { get; set; } = [];
    public IReadOnlyList<ActivityScatterPoint> Points { get; set; } = [];
}

public sealed class ActivityScatterPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public string Label { get; set; } = string.Empty;
}
