namespace TechWayFit.Pulse.Domain.Enums;

/// <summary>
/// Status of a facilitator's subscription to a plan
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and quota can be consumed
    /// </summary>
  Active = 0,

    /// <summary>
    /// Subscription was canceled by user or payment failure
    /// </summary>
    Canceled = 1,

    /// <summary>
    /// Subscription expired (past ExpiresAt date)
    /// </summary>
Expired = 2,

    /// <summary>
    /// Free trial period (future use)
    /// </summary>
  Trial = 3
}
