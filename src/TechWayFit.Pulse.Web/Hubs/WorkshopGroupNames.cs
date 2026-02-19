namespace TechWayFit.Pulse.Web.Hubs;

public static class WorkshopGroupNames
{
    public static string ForSession(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            throw new ArgumentException("Session code is required", nameof(sessionCode));
        }

        return sessionCode.Trim().ToUpperInvariant();
    }
}
