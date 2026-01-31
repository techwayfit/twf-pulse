using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Contracts.AI
{
    /// <summary>
    /// Telemetry data for AI API calls.
    /// </summary>
    public record AICallTelemetry
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("promptTokens")]
        public int PromptTokens { get; init; }

        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; init; }

        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; init; }

        [JsonPropertyName("latencyMs")]
        public long LatencyMs { get; init; }

        [JsonPropertyName("estimatedCost")]
        public decimal EstimatedCost { get; init; }

        public static decimal CalculateCost(string model, int promptTokens, int completionTokens)
        {
            // Pricing as of Jan 2026 (per 1M tokens)
            return model.ToLowerInvariant() switch
            {
                "gpt-4o" => (promptTokens * 0.0025m + completionTokens * 0.01m) / 1000,
                "gpt-4o-mini" => (promptTokens * 0.00015m + completionTokens * 0.0006m) / 1000,
                "gpt-4-turbo" => (promptTokens * 0.01m + completionTokens * 0.03m) / 1000,
                "gpt-4" => (promptTokens * 0.03m + completionTokens * 0.06m) / 1000,
                "gpt-3.5-turbo" => (promptTokens * 0.0005m + completionTokens * 0.0015m) / 1000,
                _ => 0m
            };
        }
    }
}
