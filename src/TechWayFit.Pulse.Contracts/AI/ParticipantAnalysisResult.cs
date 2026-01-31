using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Contracts.AI
{
    /// <summary>
    /// Response from AI participant analysis.
    /// </summary>
    public record ParticipantAnalysisResult
    {
        [JsonPropertyName("themes")]
        public List<Theme> Themes { get; init; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; init; } = string.Empty;

        [JsonPropertyName("sentiment")]
        public SentimentAnalysis? Sentiment { get; init; }

        [JsonPropertyName("suggestedFollowUp")]
        public string? SuggestedFollowUp { get; init; }

        [JsonPropertyName("participantCount")]
        public int? ParticipantCount { get; init; }
    }

    public record Theme
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonPropertyName("evidence")]
        public List<string> Evidence { get; init; } = new();

        [JsonPropertyName("participantCount")]
        public int? ParticipantCount { get; init; }
    }

    public record SentimentAnalysis
    {
        [JsonPropertyName("overall")]
        public string Overall { get; init; } = "neutral";

        [JsonPropertyName("intensity")]
        public double Intensity { get; init; }

        [JsonPropertyName("trend")]
        public string? Trend { get; init; }
    }
}
