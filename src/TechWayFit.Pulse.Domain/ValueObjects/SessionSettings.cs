namespace TechWayFit.Pulse.Domain.ValueObjects;

public sealed class SessionSettings
{
    public SessionSettings( 
        bool strictCurrentActivityOnly,
        bool allowAnonymous,
        int ttlMinutes)
    { 
        if (ttlMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ttlMinutes));
        }
 
        StrictCurrentActivityOnly = strictCurrentActivityOnly;
        AllowAnonymous = allowAnonymous;
        TtlMinutes = ttlMinutes;
    }



    public bool StrictCurrentActivityOnly { get; }

    public bool AllowAnonymous { get; }

    public int TtlMinutes { get; }
}
