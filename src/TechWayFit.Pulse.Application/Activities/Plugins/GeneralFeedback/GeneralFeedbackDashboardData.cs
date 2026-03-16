using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Activities.Plugins.GeneralFeedback;

/// <summary>
/// Dashboard data returned by <see cref="GeneralFeedbackActivityPlugin"/>.
/// Wraps the existing <see cref="GeneralFeedbackDashboardResponse"/> contract.
/// </summary>
public sealed class GeneralFeedbackDashboardData : IActivityDashboardData
{
    public GeneralFeedbackDashboardData(GeneralFeedbackDashboardResponse response)
    {
        Response = response;
    }

    /// <summary>The full general feedback dashboard DTO, ready for Blazor component binding.</summary>
    public GeneralFeedbackDashboardResponse Response { get; }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             => Response.SessionId;
    public Guid ActivityId            => Response.ActivityId;
    public string ActivityTitle       => Response.ActivityTitle;
    public int TotalResponses         => Response.TotalResponses;
    public int ParticipantCount       => Response.ParticipantCount;
    public int RespondedParticipants  => Response.RespondedParticipants;
    public DateTimeOffset? LastResponseAt => Response.LastResponseAt;
}
