namespace TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

 

/// <summary>Summary for plan list view</summary>
public sealed record SubscriptionPlanSummary(
    Guid Id,
    string PlanCode,
    string DisplayName,
    decimal PriceMonthly,
    int MaxSessionsPerMonth,
    bool IsActive,
    int SortOrder,
    int ActiveSubscriptionCount);

/// <summary>Detailed plan information</summary>
public sealed record SubscriptionPlanDetail(
    Guid Id,
  string PlanCode,
    string DisplayName,
    string? Description,
  decimal PriceMonthly,
    decimal? PriceYearly,
    int MaxSessionsPerMonth,
    string FeaturesJson,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int ActiveSubscriptionCount,
    int TotalSubscriptionCount);

/// <summary>Request to create a new plan</summary>
public sealed record CreateSubscriptionPlanRequest(
    string PlanCode,
    string DisplayName,
    string? Description,
    decimal PriceMonthly,
    decimal? PriceYearly,
    int MaxSessionsPerMonth,
    string FeaturesJson,
    bool IsActive,
    int SortOrder);

/// <summary>Request to update an existing plan</summary>
public sealed record UpdateSubscriptionPlanRequest(
    Guid PlanId,
    string DisplayName,
    string? Description,
    decimal PriceMonthly,
    decimal? PriceYearly,
    int MaxSessionsPerMonth,
    string FeaturesJson,
    bool IsActive,
    int SortOrder,
    string Reason);

/// <summary>Search query for plans</summary>
public sealed record PlanSearchQuery(
    bool? IsActive,
    int Page = 1,
    int PageSize = 20);

/// <summary>Search result for plans</summary>
public sealed record PlanSearchResult(
  IReadOnlyList<SubscriptionPlanSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);

 

/// <summary>Summary for subscription list view</summary>
public sealed record FacilitatorSubscriptionSummary(
    Guid Id,
    Guid FacilitatorUserId,
    string FacilitatorEmail,
  string PlanCode,
    string PlanDisplayName,
    string Status,
    int SessionsUsed,
    int SessionsAllowed,
    DateTimeOffset StartsAt,
 DateTimeOffset? ExpiresAt,
    DateTimeOffset? CanceledAt);

/// <summary>Detailed subscription information</summary>
public sealed record FacilitatorSubscriptionDetail(
    Guid Id,
    Guid FacilitatorUserId,
    string FacilitatorEmail,
    Guid PlanId,
    string PlanCode,
 string PlanDisplayName,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? CanceledAt,
  int SessionsUsed,
    DateTimeOffset SessionsResetAt,
    string? PaymentProvider,
    string? ExternalCustomerId,
    string? ExternalSubscriptionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Request to assign a plan to a facilitator (operator action)</summary>
public sealed record AssignPlanRequest(
    Guid FacilitatorUserId,
    string PlanCode,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset? ExpiresAt,
    string Reason);

/// <summary>Request to cancel a subscription</summary>
public sealed record CancelSubscriptionRequest(
    Guid SubscriptionId,
    string Reason);

/// <summary>Request to reset usage quota</summary>
public sealed record ResetQuotaRequest(
    Guid SubscriptionId,
    string Reason);

/// <summary>Search query for subscriptions</summary>
public sealed record SubscriptionSearchQuery(
    Guid? FacilitatorUserId,
    string? PlanCode,
    string? Status,
    int Page = 1,
    int PageSize = 20);

/// <summary>Search result for subscriptions</summary>
public sealed record SubscriptionSearchResult(
    IReadOnlyList<FacilitatorSubscriptionSummary> Items,
    int TotalCount,
    int Page,
int PageSize);

// ???????????????????????????????????????????????????????????????????????????
// Activity Type Definition Models
// ???????????????????????????????????????????????????????????????????????????

/// <summary>Summary for activity type list view</summary>
public sealed record ActivityTypeDefinitionSummary(
    Guid Id,
    int ActivityType,
string ActivityTypeName,
string DisplayName,
    string IconClass,
    string ColorHex,
    bool RequiresPremium,
    string? ApplicablePlanIds,
 bool IsAvailableToAllPlans,
    bool IsActive,
    int SortOrder);

/// <summary>Detailed activity type information</summary>
public sealed record ActivityTypeDefinitionDetail(
    Guid Id,
    int ActivityType,
    string ActivityTypeName,
    string DisplayName,
    string Description,
    string IconClass,
  string ColorHex,
    bool RequiresPremium,
    string? ApplicablePlanIds,
    bool IsAvailableToAllPlans,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Request to create a new activity type definition</summary>
public sealed record CreateActivityTypeDefinitionRequest(
    int ActivityType,
    string DisplayName,
    string Description,
    string IconClass,
    string ColorHex,
    bool RequiresPremium,
    string? ApplicablePlanIds,
    bool IsAvailableToAllPlans,
    bool IsActive,
    int SortOrder);

/// <summary>Request to update an activity type definition</summary>
public sealed record UpdateActivityTypeDefinitionRequest(
Guid Id,
    string DisplayName,
    string Description,
    string IconClass,
string ColorHex,
    bool RequiresPremium,
    string? ApplicablePlanIds,
    bool IsAvailableToAllPlans,
    bool IsActive,
    int SortOrder,
    string Reason);

/// <summary>Request to toggle premium status</summary>
public sealed record TogglePremiumRequest(
    Guid Id,
    bool RequiresPremium,
    string? ApplicablePlanIds,
    bool IsAvailableToAllPlans,
  string Reason);

/// <summary>Search query for activity types</summary>
public sealed record ActivityTypeSearchQuery(
    bool? IsActive,
    bool? RequiresPremium,
    int Page = 1,
    int PageSize = 50);

/// <summary>Search result for activity types</summary>
public sealed record ActivityTypeSearchResult(
    IReadOnlyList<ActivityTypeDefinitionSummary> Items,
    int TotalCount,
  int Page,
    int PageSize);
