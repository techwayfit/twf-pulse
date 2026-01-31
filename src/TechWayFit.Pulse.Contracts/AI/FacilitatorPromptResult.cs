using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Contracts.AI
{
    /// <summary>
    /// Response from AI facilitator prompt generation.
    /// </summary>
    public record FacilitatorPromptResult
    {
        [JsonPropertyName("openingStatement")]
        public string OpeningStatement { get; init; } = string.Empty;

        [JsonPropertyName("discussionQuestions")]
        public List<string> DiscussionQuestions { get; init; } = new();

        [JsonPropertyName("transitionToNextActivity")]
        public string? TransitionToNextActivity { get; init; }

        [JsonPropertyName("tone")]
        public string? Tone { get; init; }

        [JsonPropertyName("suggestedDuration")]
        public string? SuggestedDuration { get; init; }

        [JsonPropertyName("recommendations")]
        public List<ActivityRecommendation> Recommendations { get; init; } = new();
    }

    public record ActivityRecommendation
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; init; } = "medium";

        [JsonPropertyName("suggestion")]
        public object? Suggestion { get; init; }

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; init; } = string.Empty;
    }
}
