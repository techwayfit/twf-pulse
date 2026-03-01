using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.AI.Http
{
    /// <summary>
    /// Typed response from a chat/completions call.
    /// </summary>
    public sealed class OpenAIChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChatChoice> Choices { get; init; } = new List<OpenAIChatChoice>();

        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; init; }

        /// <summary>Returns the text content of the first choice, or <c>null</c> if absent.</summary>
        public string? GetContent() => Choices.Count > 0 ? Choices[0].Message?.Content : null;
    }

    /// <summary>A single completion choice returned by the API.</summary>
    public sealed class OpenAIChatChoice
    {
        [JsonPropertyName("message")]
        public OpenAIChatMessage? Message { get; init; }
    }

    /// <summary>Token usage stats reported by the API.</summary>
    public sealed class OpenAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; init; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; init; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; init; }
    }
}
