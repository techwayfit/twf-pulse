namespace TechWayFit.Pulse.Application.Services;

/// <summary>
/// Result of quota check operation
/// </summary>
public sealed record QuotaCheckResult(
    bool HasQuota,
    string Tier,
    int UsedSessions,
    int TotalSessions,
    DateTimeOffset? ResetDate,
    string? Message);

/// <summary>
/// Configuration options for AI quota limits
/// </summary>
public class AiQuotaOptions
{
    public const string SectionName = "AI:Quota";
    
    /// <summary>
    /// Number of free AI sessions per month
    /// </summary>
    public int FreeSessionsPerMonth { get; set; } = 5;
    
    /// <summary>
    /// Whether quota system is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}
