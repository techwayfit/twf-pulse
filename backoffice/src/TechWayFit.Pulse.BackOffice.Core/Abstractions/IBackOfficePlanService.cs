using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// BackOffice operations for subscription plan management.
/// SuperAdmin only - plans control the entire business model.
/// </summary>
public interface IBackOfficePlanService
{
    // ?? Plan CRUD ????????????????????????????????????????????????????????????
    
    Task<PlanSearchResult> SearchPlansAsync(PlanSearchQuery query, CancellationToken ct = default);
    
    Task<SubscriptionPlanDetail?> GetPlanDetailAsync(Guid planId, CancellationToken ct = default);
    
    Task<SubscriptionPlanDetail> CreatePlanAsync(
  CreateSubscriptionPlanRequest request,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
 
    Task UpdatePlanAsync(
        UpdateSubscriptionPlanRequest request,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
    Task TogglePlanActiveAsync(
        Guid planId,
        bool isActive,
        string reason,
      string operatorId,
        string operatorRole,
   string ipAddress,
CancellationToken ct = default);
    
    /// <summary>Get all active plans for dropdowns/selectors</summary>
    Task<IReadOnlyList<SubscriptionPlanSummary>> GetAllActivePlansAsync(CancellationToken ct = default);
    
    // ?? Plan Analytics ???????????????????????????????????????????????????????
    
    Task<IReadOnlyList<(string PlanCode, int UserCount, decimal MonthlyRevenue)>> GetRevenueSummaryAsync(
        CancellationToken ct = default);
}
