using Microsoft.Extensions.Options;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Web.Configuration;

namespace TechWayFit.Pulse.Web.Activities;

/// <summary>
/// Adapts the Web layer's <see cref="ActivityDefaultsOptions"/> (loaded from
/// appsettings.json) to the Application layer's <see cref="IActivityDefaults"/> interface.
/// <para>
/// This keeps activity plugins decoupled from the Web project's configuration classes.
/// </para>
/// </summary>
public sealed class ActivityDefaultsAdapter : IActivityDefaults
{
    private readonly ActivityDefaultsOptions _options;

    public ActivityDefaultsAdapter(IOptions<ActivityDefaultsOptions> options)
    {
        _options = options.Value;
    }

    public int PollMaxResponsesPerParticipant
        => _options.Poll.MaxResponsesPerParticipant;

    public int RatingMaxResponsesPerParticipant
        => _options.Rating.MaxResponsesPerParticipant;

    public int WordCloudMaxSubmissionsPerParticipant
        => _options.WordCloud.MaxSubmissionsPerParticipant;

    public int GeneralFeedbackMaxResponsesPerParticipant
        => _options.GeneralFeedback.MaxResponsesPerParticipant;
}
