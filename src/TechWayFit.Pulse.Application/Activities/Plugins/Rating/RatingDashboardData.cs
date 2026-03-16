using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Rating;

/// <summary>
/// Dashboard data returned by <see cref="RatingActivityPlugin"/>.
/// Wraps the existing <see cref="RatingDashboardResponse"/> contract.
/// </summary>
public sealed class RatingDashboardData : IActivityDashboardData
{
    public RatingDashboardData(RatingDashboardResponse response)
    {
        Response = response;
    }

    /// <summary>The full rating dashboard DTO, ready for Blazor component binding.</summary>
    public RatingDashboardResponse Response { get; }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             => Response.SessionId;
    public Guid ActivityId            => Response.ActivityId;
    public string ActivityTitle       => Response.ActivityTitle;
    public int TotalResponses         => Response.TotalResponses;
    public int ParticipantCount       => Response.ParticipantCount;
    public int RespondedParticipants  => Response.RespondedParticipants;
    public DateTimeOffset? LastResponseAt => Response.LastResponseAt;
}
