using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Activities.Plugins.WordCloud;

/// <summary>
/// Dashboard data returned by <see cref="WordCloudActivityPlugin"/>.
/// Wraps the existing <see cref="WordCloudDashboardResponse"/> contract.
/// </summary>
public sealed class WordCloudDashboardData : IActivityDashboardData
{
    public WordCloudDashboardData(WordCloudDashboardResponse response)
    {
        Response = response;
    }

    /// <summary>The full word cloud dashboard DTO, ready for Blazor component binding.</summary>
    public WordCloudDashboardResponse Response { get; }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             => Response.SessionId;
    public Guid ActivityId            => Response.ActivityId;
    public string ActivityTitle       => Response.ActivityTitle;
    public int TotalResponses         => Response.TotalResponses;
    public int ParticipantCount       => Response.ParticipantCount;
    public int RespondedParticipants  => Response.RespondedParticipants;
    public DateTimeOffset? LastResponseAt => Response.LastResponseAt;
}
