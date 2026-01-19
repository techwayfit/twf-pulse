namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class UpdateSessionRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public string? Context { get; set; }
}

public sealed class UpdateSessionSettingsRequest
{
    public int MaxContributionsPerParticipantPerSession { get; set; }

    public int? MaxContributionsPerParticipantPerActivity { get; set; }

    public bool StrictCurrentActivityOnly { get; set; }

    public bool AllowAnonymous { get; set; }

    public int TtlMinutes { get; set; }
}

public sealed class UpdateActivityRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public string? Config { get; set; }
}
