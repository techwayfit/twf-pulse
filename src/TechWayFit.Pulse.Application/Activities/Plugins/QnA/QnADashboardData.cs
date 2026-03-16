using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Activities.Plugins.QnA;

/// <summary>
/// Dashboard data returned by <see cref="QnAActivityPlugin"/>.
/// Wraps the existing <see cref="QnADashboardResponse"/> contract.
/// Note: <see cref="TotalResponses"/> maps to <see cref="QnADashboardResponse.TotalQuestions"/>
/// because QnA tracks questions rather than generic responses.
/// </summary>
public sealed class QnADashboardData : IActivityDashboardData
{
    public QnADashboardData(QnADashboardResponse response)
    {
        Response = response;
    }

    /// <summary>The full Q&amp;A dashboard DTO, ready for Blazor component binding.</summary>
    public QnADashboardResponse Response { get; }

    // ── IActivityDashboardData ────────────────────────────────────────────────
    public Guid SessionId             => Response.SessionId;
    public Guid ActivityId            => Response.ActivityId;
    public string ActivityTitle       => Response.ActivityTitle;
    public int TotalResponses         => Response.TotalQuestions;
    public int ParticipantCount       => Response.ParticipantCount;
    public int RespondedParticipants  => Response.RespondedParticipants;
    public DateTimeOffset? LastResponseAt => Response.LastResponseAt;
}
