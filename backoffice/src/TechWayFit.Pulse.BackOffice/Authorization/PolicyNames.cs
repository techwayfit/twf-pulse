namespace TechWayFit.Pulse.BackOffice.Authorization;

public static class PolicyNames
{
    /// <summary>Any authenticated BackOffice operator, regardless of role.</summary>
    public const string OperatorOrAbove = "OperatorOrAbove";

    /// <summary>SuperAdmin role only — for destructive or elevated actions.</summary>
    public const string SuperAdminOnly = "SuperAdminOnly";
}
