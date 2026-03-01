namespace TechWayFit.Pulse.AI.Options
{
    /// <summary>
    /// Strongly-typed options for all AI:OpenAI configuration keys.
    /// Bind via: services.Configure&lt;OpenAIOptions&gt;(config.GetSection(OpenAIOptions.SectionName))
    /// </summary>
    public class OpenAIOptions
    {
        public const string SectionName = "AI:OpenAI";

        /// <summary>API key for OpenAI or Azure OpenAI.</summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Base URL override. Leave empty for openai.com default.
        /// For Azure: https://your-resource.openai.azure.com/openai/deployments/your-deployment/
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Legacy full endpoint URL (used by FacilitatorAIService / ParticipantAIService).
        /// Defaults to https://api.openai.com/v1/chat/completions
        /// </summary>
        public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";

        /// <summary>Model name, e.g. gpt-4o-mini or gpt-4.1-mini.</summary>
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>Maximum tokens for activity generation calls.</summary>
        public int MaxTokens { get; set; } = 800;

        /// <summary>
        /// Header name used to send the API key.
        /// Leave empty for standard OpenAI (Authorization: Bearer).
        /// Set to "api-key" for Azure OpenAI.
        /// </summary>
        public string? ApiKeyHeader { get; set; }

        /// <summary>
        /// Raw query string appended to chat/completions, e.g. "api-version=2024-02-01".
        /// Leave empty for OpenAI direct; set for Azure OpenAI.
        /// </summary>
        public string? ApiQuery { get; set; }
    }
}
