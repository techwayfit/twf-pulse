using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// BackOffice operations for promo code management.
/// SuperAdmin only - promo codes grant temporary plan access.
/// </summary>
public interface IBackOfficePromoCodeService
{
    // ?? Promo Code CRUD ???????????????????????????????????????????
    
    Task<PromoCodeSearchResult> SearchPromoCodesAsync(PromoCodeSearchQuery query, CancellationToken ct = default);
    
    Task<PromoCodeDetail?> GetPromoCodeDetailAsync(Guid id, CancellationToken ct = default);
    
    Task<PromoCodeDetail> CreatePromoCodeAsync(
        CreatePromoCodeRequest request,
        string operatorId,
      string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
    Task UpdatePromoCodeAsync(
        UpdatePromoCodeRequest request,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
    Task TogglePromoCodeActiveAsync(
 Guid id,
        bool isActive,
    string reason,
     string operatorId,
    string operatorRole,
string ipAddress,
 CancellationToken ct = default);
    
    Task DeletePromoCodeAsync(
        Guid id,
        string reason,
  string operatorId,
        string operatorRole,
  string ipAddress,
        CancellationToken ct = default);
    
    // ?? Promo Code Analytics ???????????????????????????????????????
    
    Task<IReadOnlyList<PromoCodeRedemptionSummary>> GetRedemptionHistoryAsync(
      Guid promoCodeId,
        CancellationToken ct = default);
    
    Task<(int TotalRedemptions, int UniqueUsers)> GetRedemptionStatsAsync(
        Guid promoCodeId,
      CancellationToken ct = default);
}
