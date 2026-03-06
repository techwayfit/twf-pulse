using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Services;

/// <summary>
/// Service for promo code validation and redemption
/// </summary>
public sealed class PromoCodeService : IPromoCodeService
{
    private readonly IPromoCodeRepository _promoCodes;
    private readonly IFacilitatorSubscriptionRepository _subscriptions;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly ILogger<PromoCodeService> _logger;

    public PromoCodeService(
        IPromoCodeRepository promoCodes,
        IFacilitatorSubscriptionRepository subscriptions,
        ISubscriptionPlanRepository plans,
        ILogger<PromoCodeService> logger)
    {
        _promoCodes = promoCodes;
        _subscriptions = subscriptions;
        _plans = plans;
        _logger = logger;
    }

    public async Task<PromoCodeValidationResult> ValidateCodeAsync(
  string code,
     Guid userId,
        CancellationToken cancellationToken = default)
    {
  if (string.IsNullOrWhiteSpace(code))
 return new(false, "Promo code cannot be empty", null, null, null);

        var promo = await _promoCodes.GetByCodeAsync(code.Trim(), cancellationToken);

   // Check if code exists
        if (promo == null)
        {
     _logger.LogWarning("Promo code validation failed: Code '{Code}' not found (User: {UserId})", 
         code, userId);
     return new(false, "Invalid promo code", null, null, null);
      }

        // Check if active
    if (!promo.IsActive)
     {
         _logger.LogWarning("Promo code validation failed: Code '{Code}' is inactive (User: {UserId})", 
           code, userId);
        return new(false, "This promo code is no longer active", null, null, null);
      }

var now = DateTimeOffset.UtcNow;

  // Check validity period
 if (!promo.IsValidForPeriod(now))
   {
            _logger.LogWarning("Promo code validation failed: Code '{Code}' outside valid period (User: {UserId})", 
         code, userId);
            return new(false, "This promo code has expired", null, null, null);
        }

        // Check redemption limit
        if (!promo.HasRedemptionsRemaining())
        {
  _logger.LogWarning("Promo code validation failed: Code '{Code}' reached max redemptions (User: {UserId})", 
      code, userId);
            return new(false, "This promo code has reached its redemption limit", null, null, null);
        }

        // Check if user already redeemed this code
        var alreadyRedeemed = await _promoCodes.HasUserRedeemedAsync(promo.Id, userId, cancellationToken);
      if (alreadyRedeemed)
    {
  _logger.LogWarning("Promo code validation failed: User {UserId} already redeemed code '{Code}'", 
            userId, code);
            return new(false, "You have already used this promo code", null, null, null);
        }

        // Get target plan details
        var plan = await _plans.GetByIdAsync(promo.TargetPlanId, cancellationToken);
      if (plan == null)
        {
     _logger.LogError("Promo code '{Code}' references invalid plan {PlanId}", code, promo.TargetPlanId);
    return new(false, "This promo code is misconfigured. Please contact support.", null, null, null);
}

    _logger.LogInformation("Promo code '{Code}' validated successfully for user {UserId}", code, userId);
        return new(true, null, promo.TargetPlanId, plan.DisplayName, promo.DurationDays);
 }

    public async Task<FacilitatorSubscription> RedeemCodeAsync(
        string code,
        Guid userId,
     string ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Validate code first
        var validation = await ValidateCodeAsync(code, userId, cancellationToken);
      if (!validation.IsValid)
 {
   throw new InvalidOperationException(validation.ErrorMessage ?? "Invalid promo code");
   }

        var promo = await _promoCodes.GetByCodeAsync(code.Trim(), cancellationToken);
     if (promo == null)
          throw new InvalidOperationException("Promo code not found"); // Should never happen after validation

        var now = DateTimeOffset.UtcNow;

        // Expire any existing active subscription
        var oldSubscription = await _subscriptions.GetActiveSubscriptionAsync(userId, cancellationToken);
    if (oldSubscription != null)
        {
 _logger.LogInformation(
   "Expiring old subscription {SubId} (Plan: {PlanId}) for user {UserId} before applying promo code", 
                oldSubscription.Id, oldSubscription.PlanId, userId);
  oldSubscription.MarkExpired(now);
            await _subscriptions.UpdateAsync(oldSubscription, cancellationToken);
   }

        // Create new time-limited promotional subscription
        var expiresAt = now.AddDays(promo.DurationDays);
        var subscription = new FacilitatorSubscription(
            Guid.NewGuid(),
            userId,
    promo.TargetPlanId,
   SubscriptionStatus.Active,
now,
            expiresAt, // Time-limited subscription
            0, // Reset session usage
          now.AddMonths(1), // Next quota reset (1 month from now)
   null, // No payment provider (promotional)
            null, // No external customer ID
null, // No external subscription ID
       now,
  now);

        await _subscriptions.AddAsync(subscription, cancellationToken);

        // Record redemption for audit trail
   await _promoCodes.RecordRedemptionAsync(
  promo.Id,
     userId,
            subscription.Id,
    ipAddress,
     now,
            cancellationToken);

        // Increment usage counter
        promo.IncrementRedemptions(now);
        await _promoCodes.UpdateAsync(promo, cancellationToken);

        _logger.LogInformation(
   "Promo code '{Code}' redeemed by user {UserId}, created subscription {SubId} (expires: {ExpiresAt})",
            code, userId, subscription.Id, expiresAt);

        return subscription;
    }

  public async Task<PromoCode?> GetPromoCodeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _promoCodes.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(IReadOnlyList<PromoCode> PromoCodes, int TotalCount)> GetAllPromoCodesAsync(
        bool? isActive = null,
        int page = 1,
     int pageSize = 50,
      CancellationToken cancellationToken = default)
  {
    return await _promoCodes.GetAllAsync(isActive, page, pageSize, cancellationToken);
    }
}
