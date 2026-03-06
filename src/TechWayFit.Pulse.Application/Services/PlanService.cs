using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using RepoSubscriptionPlan = TechWayFit.Pulse.Application.Abstractions.Repositories.SubscriptionPlan;

namespace TechWayFit.Pulse.Application.Services;

/// <summary>
/// Service for managing subscription plans, session quotas, and feature access.
/// Handles monthly quota resets and plan-based feature gating.
/// </summary>
public sealed class PlanService : IPlanService
{
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IFacilitatorSubscriptionRepository _subscriptionRepo;
    private readonly IActivityTypeDefinitionRepository _activityTypeRepo;
    private readonly ILogger<PlanService> _logger;

    // Well-known plan code for free tier
    private const string FreePlanCode = "free";

    public PlanService(
        ISubscriptionPlanRepository planRepo,
IFacilitatorSubscriptionRepository subscriptionRepo,
        IActivityTypeDefinitionRepository activityTypeRepo,
        ILogger<PlanService> logger)
    {
        _planRepo = planRepo;
        _subscriptionRepo = subscriptionRepo;
        _activityTypeRepo = activityTypeRepo;
     _logger = logger;
    }

    public async Task<IReadOnlyList<RepoSubscriptionPlan>> GetAvailablePlansAsync(CancellationToken cancellationToken = default)
    {
        return await _planRepo.GetAllActiveAsync(cancellationToken);
    }

    public async Task<RepoSubscriptionPlan?> GetPlanByCodeAsync(string planCode, CancellationToken cancellationToken = default)
    {
        return await _planRepo.GetByCodeAsync(planCode, cancellationToken);
    }

    public async Task<PlanStatus> GetPlanStatusAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
     var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, cancellationToken);
        await ResetQuotaIfNeededAsync(subscription, cancellationToken);

        var plan = await _planRepo.GetByIdAsync(subscription.PlanId, cancellationToken)
         ?? throw new InvalidOperationException($"Plan {subscription.PlanId} not found");

 var features = PlanFeatures.FromJson(plan.FeaturesJson);

     // Build activity access map
        var activityAccess = await BuildActivityAccessMapAsync(subscription.PlanId, cancellationToken);

        var featuresDto = new PlanFeaturesDto(
    features.AiAssist,
features.AiSummary,
  activityAccess);

        return new PlanStatus(
         plan.PlanCode,
       plan.DisplayName,
   subscription.SessionsUsed,
       plan.MaxSessionsPerMonth,
        subscription.SessionsResetAt,
            subscription.ExpiresAt,
            subscription.Status.ToString(),
   featuresDto);
    }

    public async Task<FacilitatorSubscription?> GetActiveSubscriptionAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
  return await _subscriptionRepo.GetActiveSubscriptionAsync(facilitatorUserId, cancellationToken);
    }

    public async Task<bool> CanCreateSessionAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, cancellationToken);
        await ResetQuotaIfNeededAsync(subscription, cancellationToken);

    var plan = await _planRepo.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan == null)
        {
     _logger.LogWarning("Plan {PlanId} not found for user {UserId}", subscription.PlanId, facilitatorUserId);
            return false;
        }

   var canCreate = subscription.Status == SubscriptionStatus.Active
      && subscription.SessionsUsed < plan.MaxSessionsPerMonth;

        if (!canCreate)
{
        _logger.LogInformation(
        "User {UserId} cannot create session: Status={Status}, Used={Used}/{Allowed}",
    facilitatorUserId, subscription.Status, subscription.SessionsUsed, plan.MaxSessionsPerMonth);
  }

  return canCreate;
    }

    public async Task ConsumeSessionAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, cancellationToken);
        await ResetQuotaIfNeededAsync(subscription, cancellationToken);

        subscription.ConsumeSession(DateTimeOffset.UtcNow);
        await _subscriptionRepo.UpdateAsync(subscription, cancellationToken);

        _logger.LogInformation(
        "Session consumed for user {UserId}: {Used} sessions used this period",
      facilitatorUserId, subscription.SessionsUsed);
    }

 public async Task<bool> CanUseFeatureAsync(Guid facilitatorUserId, string featureCode, CancellationToken cancellationToken = default)
    {
        var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, cancellationToken);
        if (subscription.Status != SubscriptionStatus.Active)
        {
          _logger.LogDebug("User {UserId} subscription is not active: {Status}", facilitatorUserId, subscription.Status);
      return false;
        }

        var plan = await _planRepo.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan == null)
        {
         _logger.LogWarning("Plan {PlanId} not found for user {UserId}", subscription.PlanId, facilitatorUserId);
      return false;
        }

        var features = PlanFeatures.FromJson(plan.FeaturesJson);
        var hasFeature = features.HasFeature(featureCode);

        if (!hasFeature)
 {
            _logger.LogDebug(
    "User {UserId} on plan {PlanCode} cannot use feature {Feature}",
            facilitatorUserId, plan.PlanCode, featureCode);
        }

        return hasFeature;
    }

public async Task<bool> CanUseActivityTypeAsync(Guid facilitatorUserId, ActivityType activityType, CancellationToken cancellationToken = default)
    {
    var subscription = await GetOrCreateFreeSubscriptionAsync(facilitatorUserId, cancellationToken);
        if (subscription.Status != SubscriptionStatus.Active)
        {
            return false;
        }

      var activityDef = await _activityTypeRepo.GetByActivityTypeAsync(activityType, cancellationToken);
        if (activityDef == null || !activityDef.IsActive)
{
    _logger.LogDebug("Activity type {ActivityType} not found or inactive", activityType);
            return false;
        }

        // If available to all plans, allow
   if (activityDef.IsAvailableToAllPlans)
        {
         return true;
        }

// If not premium, allow for all plans
  if (!activityDef.RequiresPremium)
        {
   return true;
        }

    // Check if user's plan is in ApplicablePlanIds
        if (string.IsNullOrWhiteSpace(activityDef.ApplicablePlanIds))
        {
          // No specific plans listed but RequiresPremium=true means not available to anyone
       return false;
        }

        var allowedPlanIds = activityDef.ApplicablePlanIds.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var hasAccess = allowedPlanIds.Contains(subscription.PlanId.ToString());

        if (!hasAccess)
        {
        _logger.LogDebug(
    "User {UserId} on plan {PlanId} cannot use activity type {ActivityType}",
   facilitatorUserId, subscription.PlanId, activityType);
        }

        return hasAccess;
    }

    /// <summary>
    /// Get or auto-create Free subscription for a user if none exists
    /// </summary>
    private async Task<FacilitatorSubscription> GetOrCreateFreeSubscriptionAsync(
        Guid facilitatorUserId,
   CancellationToken cancellationToken)
    {
      var subscription = await _subscriptionRepo.GetActiveSubscriptionAsync(facilitatorUserId, cancellationToken);
        
        if (subscription != null)
   {
  return subscription;
        }

        // No active subscription — auto-assign Free plan
   _logger.LogInformation("Auto-assigning Free plan to user {UserId}", facilitatorUserId);

        var freePlan = await _planRepo.GetByCodeAsync(FreePlanCode, cancellationToken)
            ?? throw new InvalidOperationException($"Free plan '{FreePlanCode}' not found in database. Run seed scripts.");

   var now = DateTimeOffset.UtcNow;
        var newSubscription = new FacilitatorSubscription(
     id: Guid.NewGuid(),
            facilitatorUserId: facilitatorUserId,
          planId: freePlan.Id,
            status: SubscriptionStatus.Active,
            startsAt: now,
            expiresAt: null, // Free plan never expires
sessionsUsed: 0,
     sessionsResetAt: GetNextMonthStart(now),
            paymentProvider: null,
       externalCustomerId: null,
  externalSubscriptionId: null,
            createdAt: now,
     updatedAt: now);

      await _subscriptionRepo.AddAsync(newSubscription, cancellationToken);

        _logger.LogInformation(
     "Created Free subscription for user {UserId}: {MaxSessions} sessions/month",
  facilitatorUserId, freePlan.MaxSessionsPerMonth);

        return newSubscription;
    }

    /// <summary>
    /// Reset monthly session quota if reset date has passed
    /// </summary>
    private async Task ResetQuotaIfNeededAsync(
        FacilitatorSubscription subscription,
        CancellationToken cancellationToken)
    {
   var now = DateTimeOffset.UtcNow;

        if (now >= subscription.SessionsResetAt)
        {
            var nextReset = GetNextMonthStart(now);
   subscription.ResetMonthlyQuota(now, nextReset);
 await _subscriptionRepo.UpdateAsync(subscription, cancellationToken);

     _logger.LogInformation(
 "Reset session quota for user {UserId}: 0/{MaxSessions}, next reset {NextReset}",
                subscription.FacilitatorUserId, subscription.SessionsUsed, nextReset);
  }
    }

    /// <summary>
    /// Calculate next month's start date (1st of next month at 00:00 UTC)
/// </summary>
    private static DateTimeOffset GetNextMonthStart(DateTimeOffset now)
    {
        var nextMonth = now.AddMonths(1);
        return new DateTimeOffset(
      nextMonth.Year,
       nextMonth.Month,
   1, // First day of month
            0, 0, 0, // Midnight
       TimeSpan.Zero); // UTC
    }

    /// <summary>
    /// Build activity access map for all activity types based on plan
    /// </summary>
    private async Task<Dictionary<ActivityType, bool>> BuildActivityAccessMapAsync(
      Guid planId,
      CancellationToken cancellationToken)
    {
        var activityDefs = await _activityTypeRepo.GetAllActiveAsync(cancellationToken);
        var accessMap = new Dictionary<ActivityType, bool>();

        foreach (var def in activityDefs)
      {
// If available to all plans, grant access
    if (def.IsAvailableToAllPlans)
    {
     accessMap[def.ActivityType] = true;
                continue;
      }

       // If not premium, grant access to all
      if (!def.RequiresPremium)
            {
    accessMap[def.ActivityType] = true;
  continue;
        }

    // Check if plan is in ApplicablePlanIds
    var hasAccess = false;
            if (!string.IsNullOrWhiteSpace(def.ApplicablePlanIds))
        {
     var allowedPlanIds = def.ApplicablePlanIds.Split('|', StringSplitOptions.RemoveEmptyEntries);
        hasAccess = allowedPlanIds.Contains(planId.ToString());
  }

accessMap[def.ActivityType] = hasAccess;
        }

        return accessMap;
    }
}
