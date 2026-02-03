namespace TechWayFit.Pulse.Application.Context;

/// <summary>
/// Represents the current facilitator context for the request
/// </summary>
public sealed class FacilitatorContext
{
    public Guid FacilitatorUserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? OpenAiApiKey { get; init; }
    public string? OpenAiBaseUrl { get; init; }
}
