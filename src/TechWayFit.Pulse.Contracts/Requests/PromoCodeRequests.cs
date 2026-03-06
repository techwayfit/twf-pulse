namespace TechWayFit.Pulse.Contracts.Requests;

/// <summary>
/// Request to validate a promo code
/// </summary>
public sealed record ValidatePromoCodeRequest(string Code);

/// <summary>
/// Request to redeem a promo code
/// </summary>
public sealed record RedeemPromoCodeRequest(string Code);
