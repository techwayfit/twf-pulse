using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechWayFit.Pulse.AI.Http;
using TechWayFit.Pulse.AI.Options;
using TechWayFit.Pulse.AI.Prompts;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.AI.Services
{
    public class ParticipantAIService : IParticipantAIService
    {
        private readonly OpenAIApiClient _aiClient;
        private readonly OpenAIOptions _openAIOptions;
        private readonly ILogger<ParticipantAIService> _logger;

        public ParticipantAIService(
            OpenAIApiClient aiClient,
            IOptions<OpenAIOptions> openAIOptions,
            ILogger<ParticipantAIService> logger)
        {
            _aiClient = aiClient;
            _openAIOptions = openAIOptions.Value;
            _logger = logger;
        }

        public async Task<(ParticipantAnalysisResult? Result, AICallTelemetry? Telemetry)> AnalyzeParticipantResponsesAsync(
            Guid sessionId,
            Guid activityId,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _openAIOptions.ApiKey;
            var model  = _openAIOptions.Model;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured - returning mock participant analysis.");
                return (new ParticipantAnalysisResult { Summary = "(mock) Analysis unavailable - OpenAI API key missing" }, null);
            }

            var request = OpenAIChatRequest.Build(model)
                .WithMessages(new[]
                {
                    OpenAIChatMessage.System(PromptConstants.ParticipantAnalysis.SystemPrompt),
                    OpenAIChatMessage.User(string.Format(PromptConstants.ParticipantAnalysis.UserPromptTemplate, sessionId, activityId))
                })
                .WithTemperature(0.5)
                .WithMaxTokens(800)
                .Create();

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var chatResponse = await _aiClient.PostChatAsync(apiKey, _openAIOptions.BaseUrl, request, cancellationToken);
                stopwatch.Stop();

                AICallTelemetry? telemetry = null;
                if (chatResponse.Usage != null)
                {
                    var u = chatResponse.Usage;
                    telemetry = new AICallTelemetry
                    {
                        Model            = model,
                        PromptTokens     = u.PromptTokens,
                        CompletionTokens = u.CompletionTokens,
                        TotalTokens      = u.TotalTokens,
                        LatencyMs        = stopwatch.ElapsedMilliseconds,
                        EstimatedCost    = AICallTelemetry.CalculateCost(model, u.PromptTokens, u.CompletionTokens)
                    };
                    _logger.LogInformation(
                        "AI participant analysis - Session: {Session}, Activity: {Activity}, Model: {Model}, Tokens: {Total}, Cost: ${Cost:F4}, Latency: {Latency}ms",
                        sessionId, activityId, model, u.TotalTokens, telemetry.EstimatedCost, stopwatch.ElapsedMilliseconds);
                }

                var jsonText = chatResponse.GetContent() ?? string.Empty;
                try
                {
                    var result = JsonSerializer.Deserialize<ParticipantAnalysisResult>(jsonText);
                    return (result, telemetry);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize AI response to ParticipantAnalysisResult");
                    return (new ParticipantAnalysisResult { Summary = jsonText }, telemetry);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to call OpenAI for participant analysis after {Latency}ms", stopwatch.ElapsedMilliseconds);
                return (new ParticipantAnalysisResult { Summary = $"Error: {ex.Message}" }, null);
            }
        }
    }
}
