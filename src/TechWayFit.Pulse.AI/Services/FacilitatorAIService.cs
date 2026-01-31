using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.AI.Services
{
    public class FacilitatorAIService : IFacilitatorAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FacilitatorAIService> _logger;

        public FacilitatorAIService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FacilitatorAIService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(FacilitatorPromptResult? Result, AICallTelemetry? Telemetry)> GenerateFacilitatorPromptAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["AI:OpenAI:ApiKey"];
            var endpoint = _configuration["AI:OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            var model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured - returning mock facilitator prompt.");
                return (new FacilitatorPromptResult { OpeningStatement = "(mock) Facilitator prompt unavailable - OpenAI API key missing" }, null);
            }

            var stopwatch = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient("openai");

            var systemPrompt = @"You are an expert workshop facilitator helping to guide discussions.
Return a JSON object with this structure:
{
  ""openingStatement"": ""A brief opening statement to frame the discussion"",
  ""discussionQuestions"": [""Question 1?"", ""Question 2?"", ""Question 3?""],
  ""transitionToNextActivity"": ""Optional transition statement"",
  ""tone"": ""empathetic_and_action_oriented|professional|casual"",
  ""suggestedDuration"": ""5-7 minutes"",
  ""recommendations"": []
}";

            var userPrompt = $"Generate a facilitator opening statement and 3 discussion questions for session {sessionId} activity {activityId}. Help the facilitator guide a productive discussion.";

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,
                max_tokens = 600
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var resp = await client.PostAsync(endpoint, content, cancellationToken);
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                
                stopwatch.Stop();

                using var doc = JsonDocument.Parse(body);
                
                // Extract telemetry
                AICallTelemetry? telemetry = null;
                if (doc.RootElement.TryGetProperty("usage", out var usage))
                {
                    var promptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
                    var completionTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;
                    var totalTokens = usage.TryGetProperty("total_tokens", out var tt) ? tt.GetInt32() : 0;

                    telemetry = new AICallTelemetry
                    {
                        Model = model,
                        PromptTokens = promptTokens,
                        CompletionTokens = completionTokens,
                        TotalTokens = totalTokens,
                        LatencyMs = stopwatch.ElapsedMilliseconds,
                        EstimatedCost = AICallTelemetry.CalculateCost(model, promptTokens, completionTokens)
                    };

                    _logger.LogInformation(
                        "AI facilitator prompt - Session: {Session}, Activity: {Activity}, Model: {Model}, Tokens: {Total}, Cost: ${Cost:F4}, Latency: {Latency}ms",
                        sessionId, activityId, model, totalTokens, telemetry.EstimatedCost, stopwatch.ElapsedMilliseconds);
                }

                // Extract content
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentEl))
                    {
                        var jsonText = contentEl.GetString() ?? string.Empty;
                        try
                        {
                            var result = JsonSerializer.Deserialize<FacilitatorPromptResult>(jsonText);
                            return (result, telemetry);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize AI response to FacilitatorPromptResult");
                            return (new FacilitatorPromptResult { OpeningStatement = jsonText }, telemetry);
                        }
                    }
                }

                return (new FacilitatorPromptResult { OpeningStatement = "(no content in response)" }, telemetry);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to call OpenAI for facilitator prompt generation after {Latency}ms", stopwatch.ElapsedMilliseconds);
                return (new FacilitatorPromptResult { OpeningStatement = $"Error: {ex.Message}" }, null);
            }
        }
    }
}
