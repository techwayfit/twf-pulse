using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Quadrant;

/// <summary>
/// Dashboard data returned by <see cref="QuadrantActivityPlugin"/>.
/// Wraps the existing <see cref="QuadrantDashboardResponse"/> contract.
/// </summary>
public sealed class QuadrantDashboardData : IActivityDashboardData
{
    public QuadrantDashboardData(QuadrantDashboardResponse response)
    {
        Response = response;
    }

    /// <summary>The full quadrant dashboard DTO, ready for Blazor component binding.</summary>
    public QuadrantDashboardResponse Response { get; }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             => Response.SessionId;
    public Guid ActivityId            => Response.ActivityId;
    public string ActivityTitle       => Response.ActivityTitle;
    public int TotalResponses         => Response.TotalResponses;
    public int ParticipantCount       => Response.ParticipantCount;
    public int RespondedParticipants  => Response.RespondedParticipants;
    public DateTimeOffset? LastResponseAt => Response.LastResponseAt;
}
