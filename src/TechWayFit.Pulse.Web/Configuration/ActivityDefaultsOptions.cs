namespace TechWayFit.Pulse.Web.Configuration;

public class ActivityDefaultsOptions
{
    public const string SectionName = "ActivityDefaults";

    public PollDefaultsOptions Poll { get; set; } = new();
    public RatingDefaultsOptions Rating { get; set; } = new();
    public WordCloudDefaultsOptions WordCloud { get; set; } = new();
    public GeneralFeedbackDefaultsOptions GeneralFeedback { get; set; } = new();
}

public class PollDefaultsOptions
{
    public int MaxResponsesPerParticipant { get; set; } = 1;
}

public class RatingDefaultsOptions
{
    public int MaxResponsesPerParticipant { get; set; } = 1;
}

public class WordCloudDefaultsOptions
{
    public int MaxSubmissionsPerParticipant { get; set; } = 3;
}

public class GeneralFeedbackDefaultsOptions
{
    public int MaxResponsesPerParticipant { get; set; } = 5;
}

public class ContextDocumentLimitsOptions
{
    public const string SectionName = "ContextDocumentLimits";

    public int SprintBacklogSummaryMaxChars { get; set; } = 1000;
    public int IncidentSummaryMaxChars { get; set; } = 1000;
    public int ProductSummaryMaxChars { get; set; } = 5000;
}
