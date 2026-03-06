namespace TechWayFit.Pulse.Contracts.Responses;

/// <summary>
/// Response from promo code validation
/// </summary>
public sealed record ValidatePromoCodeResponse(
    bool IsValid,
    string? ErrorMessage,
    string? TargetPlanDisplayName,
    int? DurationDays);

/// <summary>
/// Response from promo code redemption
/// </summary>
public sealed record RedeemPromoCodeResponse(
    Guid SubscriptionId,
    Guid PlanId,
    DateTimeOffset ExpiresAt,
    string Message);
