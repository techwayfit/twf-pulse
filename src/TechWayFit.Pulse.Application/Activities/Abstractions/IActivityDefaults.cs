namespace TechWayFit.Pulse.Application.Activities.Abstractions;

/// <summary>
/// Application-level abstraction over activity limit configuration.
/// Keeps activity plugins decoupled from the Web layer's ActivityDefaultsOptions.
/// The Web layer registers an adapter that wraps ActivityDefaultsOptions.
/// </summary>
public interface IActivityDefaults
{
    int PollMaxResponsesPerParticipant { get; }
    int RatingMaxResponsesPerParticipant { get; }
    int WordCloudMaxSubmissionsPerParticipant { get; }
    int GeneralFeedbackMaxResponsesPerParticipant { get; }
}
