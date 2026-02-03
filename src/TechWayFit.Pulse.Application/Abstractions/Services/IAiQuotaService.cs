using TechWayFit.Pulse.Application.Services;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Service for managing AI usage quotas for facilitators
/// </summary>
public interface IAiQuotaService
{
    /// <summary>
    /// Check if a facilitator has quota available for AI generation
    /// </summary>
    Task<QuotaCheckResult> CheckQuotaAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Consume one AI session from the facilitator's quota
    /// </summary>
    Task ConsumeQuotaAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the current quota status for a facilitator
    /// </summary>
    Task<QuotaCheckResult> GetQuotaStatusAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reset quota if the reset date has passed
    /// </summary>
    Task ResetQuotaIfNeededAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
}
