namespace TechWayFit.Pulse.Contracts.Responses;

/// <summary>
/// Plan status and quota information for the current user
/// </summary>
public sealed record PlanStatusResponse(
    string PlanCode,
    string PlanDisplayName,
    int SessionsUsed,
    int SessionsAllowed,
    DateTimeOffset SessionsResetAt,
    DateTimeOffset? ExpiresAt,
    string Status,
    FeatureAccessDto Features);

/// <summary>
/// Feature access flags for the current plan
/// </summary>
public sealed record FeatureAccessDto(
    bool AiAssist,
    bool AiSummary,
    Dictionary<string, bool> ActivityAccess);
