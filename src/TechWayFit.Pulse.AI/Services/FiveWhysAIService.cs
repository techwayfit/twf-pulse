using System;
using System.Collections.Generic;
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
    public class FiveWhysAIService : IFiveWhysAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FiveWhysAIService> _logger;

        public FiveWhysAIService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<FiveWhysAIService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<FiveWhysNextStepResult> GetNextStepAsync(
            string rootQuestion,
            string? context,
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth = 5,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["AI:OpenAI:ApiKey"];
            var model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured — returning mock 5 Whys step.");
                return BuildMockStep(chain, maxDepth, rootQuestion);
            }

            var systemPrompt = BuildSystemPrompt(maxDepth);
            var userPrompt = BuildUserPrompt(rootQuestion, context, chain, maxDepth);

            var payload = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.6,
                max_tokens = 400,
                response_format = new { type = "json_object" }
            };

            var client = _httpClientFactory.CreateClient("openai");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestJson = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await client.PostAsync("chat/completions", httpContent, cancellationToken);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                stopwatch.Stop();

                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var messageContent = choices[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "{}";

                    _logger.LogDebug("5 Whys AI raw response: {Response}", messageContent);

                    var result = JsonSerializer.Deserialize<FiveWhysNextStepResult>(
                        messageContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return result ?? BuildFallbackStep(chain, maxDepth, rootQuestion);
                }

                return BuildFallbackStep(chain, maxDepth, rootQuestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "5 Whys AI call failed at depth {Depth}", chain.Count + 1);
                return BuildFallbackStep(chain, maxDepth, rootQuestion);
            }
        }

        private static string BuildSystemPrompt(int maxDepth) => $@"You are a Socratic strategy coach running a '5 Whys' root cause analysis in a workshop.
Your job is to help the participant dig deeper to find the TRUE root cause of a problem.

Rules:
1. If the participant's last answer reveals a ROOT CAUSE (i.e., a foundational issue you cannot dig further into meaningfully, OR you have reached depth {maxDepth}), set isComplete=true and provide rootCause and insight.
2. Otherwise, ask ONE precise follow-up question that starts with ""Why"" or ""What caused"" to push deeper.
3. Your follow-up question must directly address the specific reason given in the last answer — not the original problem.
4. Be concise. One sentence per question. No preamble.
5. A root cause is typically a process gap, missing capability, structural issue, or human factor — not just a surface symptom.

Respond ONLY with valid JSON in this exact format:
{{
  ""nextQuestion"": ""<follow-up question, or null if complete>"",
  ""isComplete"": <true|false>,
  ""rootCause"": ""<one-sentence root cause statement, or null>"",
  ""insight"": ""<2-3 sentence insight explaining the root cause and suggesting a concrete action, or null>""
}}";

        private static string BuildUserPrompt(
            string rootQuestion,
            string? context,
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"ORIGINAL PROBLEM QUESTION: {rootQuestion}");

            if (!string.IsNullOrWhiteSpace(context))
                sb.AppendLine($"BACKGROUND CONTEXT: {context}");

            sb.AppendLine();
            sb.AppendLine("CONVERSATION CHAIN SO FAR:");

            foreach (var entry in chain)
            {
                sb.AppendLine($"  Level {entry.Level} Q: {entry.Question}");
                sb.AppendLine($"  Level {entry.Level} A: {entry.Answer}");
            }

            sb.AppendLine();
            sb.AppendLine($"Current depth: {chain.Count} / {maxDepth}");

            if (chain.Count >= maxDepth)
                sb.AppendLine("NOTE: Maximum depth reached. You MUST set isComplete=true and provide a root cause now.");

            sb.AppendLine();
            sb.AppendLine("Based on the last answer, should we dig deeper or have we found the root cause? Respond in JSON.");

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
                    RootCause = "Insufficient process documentation and lack of clear ownership for this area.",
                    Insight = "The root cause appears to be a systemic gap in process ownership. Consider assigning a clear DRI (Directly Responsible Individual) and documenting the process end-to-end to prevent recurrence."
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
