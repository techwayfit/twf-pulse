namespace TechWayFit.Pulse.Application.DTOs;

/// <summary>
/// Lightweight DTO for session summary information.
/// Used for dashboard widgets and listings where full session details are not needed.
/// </summary>
public sealed class SessionSummaryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
  public string Title { get; init; } = string.Empty;
    public Domain.Enums.SessionStatus Status { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTime? SessionStart { get; init; }
    public DateTime? SessionEnd { get; init; }
}
