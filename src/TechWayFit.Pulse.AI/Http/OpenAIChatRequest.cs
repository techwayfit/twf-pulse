using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.AI.Http
{
    /// <summary>
    /// Typed payload for a chat/completions request.
    /// Use <see cref="OpenAIChatRequest.Build"/> for a fluent construction.
    /// </summary>
    public sealed class OpenAIChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public OpenAIChatMessage[] Messages { get; init; } = new OpenAIChatMessage[0];

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; } = 0.7;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; init; } = 800;

        [JsonPropertyName("stream")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Stream { get; init; }

        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenAIResponseFormat? ResponseFormat { get; init; }

        // ── Fluent builder ────────────────────────────────────────────────────

        public static Builder Build(string model) => new(model);

        public sealed class Builder(string model)
        {
            private OpenAIChatMessage[] _messages = new OpenAIChatMessage[0];
            private double _temperature = 0.7;
            private int _maxTokens = 800;
            private bool? _stream;
            private OpenAIResponseFormat? _responseFormat;

            public Builder WithMessages(OpenAIChatMessage[] messages)   { _messages = messages;         return this; }
            public Builder WithTemperature(double temp)                 { _temperature = temp;          return this; }
            public Builder WithMaxTokens(int tokens)                    { _maxTokens = tokens;          return this; }
            public Builder WithStream(bool stream = true)               { _stream = stream;             return this; }
            public Builder WithJsonResponseFormat()                     { _responseFormat = OpenAIResponseFormat.JsonObject; return this; }

            public OpenAIChatRequest Create() => new()
            {
                Model          = model,
                Messages       = _messages,
                Temperature    = _temperature,
                MaxTokens      = _maxTokens,
                Stream         = _stream,
                ResponseFormat = _responseFormat
            };
        }
    }

    /// <summary>A single chat message with a role and text content.</summary>
    public sealed class OpenAIChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        public static OpenAIChatMessage System(string content) => new() { Role = "system", Content = content };
        public static OpenAIChatMessage User(string content)   => new() { Role = "user",   Content = content };
    }

    /// <summary>
    /// Constrains the response format. Use <see cref="JsonObject"/> for structured JSON output.
    /// </summary>
    public sealed class OpenAIResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "text";

        /// <summary>Instructs the model to return valid JSON (GPT-4o / gpt-3.5-turbo-1106+).</summary>
        public static readonly OpenAIResponseFormat JsonObject = new() { Type = "json_object" };

        /// <summary>Default plain-text response.</summary>
        public static readonly OpenAIResponseFormat Text = new() { Type = "text" };
    }
}
