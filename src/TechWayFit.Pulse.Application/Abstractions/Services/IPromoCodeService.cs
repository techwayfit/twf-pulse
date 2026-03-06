using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Service for promo code validation and redemption
/// </summary>
public interface IPromoCodeService
{
    /// <summary>
    /// Validate a promo code without redeeming it
  /// </summary>
    Task<PromoCodeValidationResult> ValidateCodeAsync(
        string code,
        Guid userId,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeem a promo code and assign promotional subscription to user
    /// </summary>
    Task<FacilitatorSubscription> RedeemCodeAsync(
        string code,
        Guid userId,
        string ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
 /// Get promo code by ID (for BackOffice)
    /// </summary>
  Task<PromoCode?> GetPromoCodeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all promo codes with pagination (for BackOffice)
    /// </summary>
    Task<(IReadOnlyList<PromoCode> PromoCodes, int TotalCount)> GetAllPromoCodesAsync(
    bool? isActive = null,
        int page = 1,
        int pageSize = 50,
      CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of promo code validation
/// </summary>
public sealed record PromoCodeValidationResult(
    bool IsValid,
    string? ErrorMessage,
    Guid? TargetPlanId,
    string? PlanDisplayName,
    int? DurationDays);
