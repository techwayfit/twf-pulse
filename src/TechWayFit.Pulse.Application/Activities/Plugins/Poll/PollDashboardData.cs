using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Poll;

/// <summary>
/// Dashboard data returned by <see cref="PollActivityPlugin"/>.
/// Wraps the existing <see cref="PollDashboardResponse"/> contract.
/// The common <see cref="IActivityDashboardData"/> properties delegate to the
/// wrapped response so the Web layer can handle any activity generically,
/// while still casting to <see cref="PollDashboardData"/> for poll-specific fields.
/// </summary>
public sealed class PollDashboardData : IActivityDashboardData
{
    public PollDashboardData(PollDashboardResponse response)
    {
        Response = response;
    }

    /// <summary>The full poll dashboard DTO, ready for Blazor component binding.</summary>
    public PollDashboardResponse Response { get; }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             => Response.SessionId;
    public Guid ActivityId            => Response.ActivityId;
    public string ActivityTitle       => Response.ActivityTitle;
    public int TotalResponses         => Response.TotalResponses;
    public int ParticipantCount       => Response.ParticipantCount;
    public int RespondedParticipants  => Response.RespondedParticipants;
    public DateTimeOffset? LastResponseAt => Response.LastResponseAt;
}
