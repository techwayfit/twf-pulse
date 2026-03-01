using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
    public class FiveWhysAIService : IFiveWhysAIService
    {
        private readonly OpenAIApiClient _aiClient;
        private readonly OpenAIOptions _openAIOptions;
        private readonly ILogger<FiveWhysAIService> _logger;

        public FiveWhysAIService(
            OpenAIApiClient aiClient,
            IOptions<OpenAIOptions> openAIOptions,
            ILogger<FiveWhysAIService> logger)
        {
            _aiClient = aiClient;
            _openAIOptions = openAIOptions.Value;
            _logger = logger;
        }

        public async Task<FiveWhysNextStepResult> GetNextStepAsync(
            string rootQuestion,
            string? context,
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth = 5,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _openAIOptions.ApiKey;
            var model  = _openAIOptions.Model;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured — returning mock 5 Whys step.");
                return BuildMockStep(chain, maxDepth, rootQuestion);
            }

            var chatRequest = OpenAIChatRequest.Build(model)
                .WithMessages(new[]
                {
                    OpenAIChatMessage.System(PromptConstants.FiveWhys.GetSystemPrompt(maxDepth)),
                    OpenAIChatMessage.User(BuildUserPrompt(rootQuestion, context, chain, maxDepth))
                })
                .WithTemperature(0.6)
                .WithMaxTokens(400)
                .WithJsonResponseFormat()
                .Create();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var chatResponse = await _aiClient.PostChatAsync(apiKey, _openAIOptions.BaseUrl, chatRequest, cancellationToken);
                stopwatch.Stop();

                var messageContent = chatResponse.GetContent() ?? "{}";
                _logger.LogDebug("5 Whys AI raw response: {Response}", messageContent);

                var result = JsonSerializer.Deserialize<FiveWhysNextStepResult>(
                    messageContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result ?? BuildFallbackStep(chain, maxDepth, rootQuestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "5 Whys AI call failed at depth {Depth}", chain.Count + 1);
                return BuildFallbackStep(chain, maxDepth, rootQuestion);
            }
        }

        private static string BuildUserPrompt(
            string rootQuestion,
            string? context,
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{PromptConstants.FiveWhys.UserPromptOriginalProblem}{rootQuestion}");

            if (!string.IsNullOrWhiteSpace(context))
                sb.AppendLine($"{PromptConstants.FiveWhys.UserPromptBackgroundContext}{context}");

            sb.AppendLine();
            sb.AppendLine(PromptConstants.FiveWhys.UserPromptChainHeader);

            foreach (var entry in chain)
            {
                sb.AppendLine($"  Level {entry.Level} Q: {entry.Question}");
                sb.AppendLine($"  Level {entry.Level} A: {entry.Answer}");
            }

            sb.AppendLine();
            sb.AppendLine($"Current depth: {chain.Count} / {maxDepth}");

            if (chain.Count >= maxDepth)
                sb.AppendLine(PromptConstants.FiveWhys.UserPromptMaxDepthReached);

            sb.AppendLine();
            sb.AppendLine(PromptConstants.FiveWhys.UserPromptDecisionRequest);

            return sb.ToString();
        }

        private static FiveWhysNextStepResult BuildMockStep(
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth,
            string rootQuestion)
        {
            if (chain.Count >= maxDepth)
            {
                return new FiveWhysNextStepResult
                {
                    IsComplete = true,
                    RootCause = PromptConstants.FiveWhys.FallbackRootCause,
                    Insight = PromptConstants.FiveWhys.FallbackInsight
                };
            }

            var mockQuestions = new[]
            {
                $"Why did that specific situation arise?",
                "What caused that to happen in the first place?",
                "Why was that constraint not identified earlier?",
                "What process or structure allowed this gap to exist?"
            };

            var nextQ = chain.Count < mockQuestions.Length
                ? mockQuestions[chain.Count]
                : "Why was this not caught before it became a problem?";

            return new FiveWhysNextStepResult
            {
                NextQuestion = nextQ,
                IsComplete = false
            };
        }

        private static FiveWhysNextStepResult BuildFallbackStep(
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth,
            string rootQuestion) =>
            BuildMockStep(chain, maxDepth, rootQuestion);
    }
}
