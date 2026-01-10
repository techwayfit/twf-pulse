namespace TechWayFit.Pulse.Contracts.Models;

public sealed class SessionSettingsDto
{
    public int MaxContributionsPerParticipantPerSession { get; set; }

    public int? MaxContributionsPerParticipantPerActivity { get; set; }

    public bool StrictCurrentActivityOnly { get; set; }

    public bool AllowAnonymous { get; set; }

    public int TtlMinutes { get; set; }
}
