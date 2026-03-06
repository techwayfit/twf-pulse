using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

/// <summary>
/// Repository for promo code management and validation
/// </summary>
public interface IPromoCodeRepository
{
    /// <summary>
    /// Get promo code by code string (case-insensitive)
    /// </summary>
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get promo code by ID
    /// </summary>
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all promo codes with optional filters
    /// </summary>
    Task<(IReadOnlyList<PromoCode> PromoCodes, int TotalCount)> GetAllAsync(
   bool? isActive = null,
        int page = 1,
        int pageSize = 50,
     CancellationToken cancellationToken = default);

  /// <summary>
    /// Check if a user has already redeemed a specific promo code
    /// </summary>
    Task<bool> HasUserRedeemedAsync(Guid promoCodeId, Guid userId, CancellationToken cancellationToken = default);

/// <summary>
 /// Get redemption history for a promo code
    /// </summary>
    Task<IReadOnlyList<PromoCodeRedemption>> GetRedemptionsAsync(
        Guid promoCodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's redemption history
 /// </summary>
    Task<IReadOnlyList<PromoCodeRedemption>> GetUserRedemptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a promo code redemption
    /// </summary>
    Task RecordRedemptionAsync(
        Guid promoCodeId,
  Guid userId,
        Guid subscriptionId,
 string ipAddress,
        DateTimeOffset redeemedAt,
 CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new promo code
    /// </summary>
    Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing promo code
    /// </summary>
  Task UpdateAsync(PromoCode promoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete promo code (only if never redeemed)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
