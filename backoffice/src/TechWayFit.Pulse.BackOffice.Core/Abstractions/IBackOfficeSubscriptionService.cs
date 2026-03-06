using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// BackOffice operations for facilitator subscription management.
/// Operators can assign plans manually; SuperAdmins have full access.
/// </summary>
public interface IBackOfficeSubscriptionService
{
    // ?? Subscription Search & Detail ?????????????????????????????????????????
    
    Task<SubscriptionSearchResult> SearchSubscriptionsAsync(
        SubscriptionSearchQuery query,
        CancellationToken ct = default);
    
    Task<FacilitatorSubscriptionDetail?> GetSubscriptionDetailAsync(
        Guid subscriptionId,
  CancellationToken ct = default);
    
    Task<IReadOnlyList<FacilitatorSubscriptionSummary>> GetUserSubscriptionHistoryAsync(
     Guid facilitatorUserId,
        CancellationToken ct = default);
    
    // ?? Manual Plan Assignment (Operator Action) ?????????????????????????????
    
    /// <summary>Assign a plan to a user (comps, trials, support)</summary>
    Task<FacilitatorSubscriptionDetail> AssignPlanAsync(
        AssignPlanRequest request,
        string operatorId,
        string operatorRole,
        string ipAddress,
CancellationToken ct = default);
    
    /// <summary>Cancel an active subscription</summary>
    Task CancelSubscriptionAsync(
        CancelSubscriptionRequest request,
        string operatorId,
   string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
    /// <summary>Reset monthly usage quota (support action)</summary>
    Task ResetQuotaAsync(
        ResetQuotaRequest request,
      string operatorId,
   string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
    // ?? Subscription Analytics ???????????????????????????????????????????????
    
    Task<(int Active, int Canceled, int Expired, int Trial)> GetSubscriptionStatusCountsAsync(
      CancellationToken ct = default);
}
