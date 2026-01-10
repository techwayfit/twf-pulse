namespace TechWayFit.Pulse.Domain.ValueObjects;

public sealed class SessionSettings
{
    public SessionSettings(
        int maxContributionsPerParticipantPerSession,
        int? maxContributionsPerParticipantPerActivity,
        bool strictCurrentActivityOnly,
        bool allowAnonymous,
        int ttlMinutes)
    {
        if (maxContributionsPerParticipantPerSession <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxContributionsPerParticipantPerSession));
        }

        if (maxContributionsPerParticipantPerActivity.HasValue && maxContributionsPerParticipantPerActivity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxContributionsPerParticipantPerActivity));
        }

        if (ttlMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ttlMinutes));
        }

        MaxContributionsPerParticipantPerSession = maxContributionsPerParticipantPerSession;
        MaxContributionsPerParticipantPerActivity = maxContributionsPerParticipantPerActivity;
        StrictCurrentActivityOnly = strictCurrentActivityOnly;
        AllowAnonymous = allowAnonymous;
        TtlMinutes = ttlMinutes;
    }

    public int MaxContributionsPerParticipantPerSession { get; }

    public int? MaxContributionsPerParticipantPerActivity { get; }

    public bool StrictCurrentActivityOnly { get; }

    public bool AllowAnonymous { get; }

    public int TtlMinutes { get; }
}
