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
using TechWayFit.Pulse.AI.Utilities;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Contracts.Enums;

namespace TechWayFit.Pulse.AI.Services
{
    public class SessionAIService : ISessionAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SessionAIService> _logger;
        private readonly MockSessionAIService _mock = new MockSessionAIService();

        public SessionAIService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SessionAIService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["AI:OpenAI:ApiKey"];
            var endpoint = _configuration["AI:OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            var model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured - using mock session generator");
                return await _mock.GenerateSessionActivitiesAsync(request, cancellationToken);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var client = _httpClientFactory.CreateClient("openai");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Build improved system prompt with schema
                var systemPrompt = BuildSystemPrompt();
                
                // Build enhanced user prompt with generation context
                var userPrompt = BuildUserPrompt(request);

                var payload = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = request.GenerationOptions?.Temperature ?? 0.7,
                    max_tokens = _configuration.GetValue<int?>("AI:OpenAI:MaxTokens") ?? 800
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(endpoint, content, cancellationToken);
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync(cancellationToken);

                stopwatch.Stop();

                // Try to extract the model's content and log telemetry
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    
                    // Extract token usage for telemetry
                    if (doc.RootElement.TryGetProperty("usage", out var usage))
                    {
                        var promptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
                        var completionTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;
                        var totalTokens = usage.TryGetProperty("total_tokens", out var tt) ? tt.GetInt32() : 0;

                        _logger.LogInformation(
                            "AI session generation completed - Model: {Model}, Tokens: {Total} ({Prompt} prompt + {Completion} completion), Latency: {Latency}ms",
                            model, totalTokens, promptTokens, completionTokens, stopwatch.ElapsedMilliseconds);
                    }
                    
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var first = choices[0];
                        if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentEl))
                        {
                            var jsonText = contentEl.GetString() ?? string.Empty;
                            return ParseActivitiesJson(jsonText);
                        }
                        else if (first.TryGetProperty("text", out var textEl))
                        {
                            var jsonText = textEl.GetString() ?? string.Empty;
                            return ParseActivitiesJson(jsonText);
                        }
                    }

                    // Fallback: body itself might be JSON array
                    return ParseActivitiesJson(body);
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Failed to parse OpenAI response as JSON, falling back to mock");
                    return await _mock.GenerateSessionActivitiesAsync(request, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "SessionAIService: OpenAI call failed after {Latency}ms, falling back to mock", stopwatch.ElapsedMilliseconds);
                return await _mock.GenerateSessionActivitiesAsync(request, cancellationToken);
            }
        }

        private string BuildUserPrompt(CreateSessionRequest request)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Generate a session agenda as a JSON array of activities.");
            prompt.AppendLine("Return an array where each item has: type (Poll|WordCloud|Rating|GeneralFeedback), title, prompt, durationMinutes (integer), optional config (object).\n");
            
            // Basic info
            prompt.AppendLine($"Session title: {request.Title}");
            prompt.AppendLine($"Goal: {request.Goal}");
            
            // Use enhanced context if available, otherwise fallback to legacy
            if (request.GenerationContext != null)
            {
                var ctx = request.GenerationContext;
                
                // Workshop type and duration
                if (!string.IsNullOrEmpty(ctx.WorkshopType))
                {
                    prompt.AppendLine($"Workshop type: {ctx.WorkshopType}");
                }
                if (ctx.DurationMinutes > 0)
                {
                    prompt.AppendLine($"Total session duration: {ctx.DurationMinutes} minutes");
                    var suggestedCount = CalculateActivityCount(ctx.DurationMinutes.Value);
                    prompt.AppendLine($"Generate {suggestedCount} activities to fit this timeframe.");
                }
                if (ctx.ParticipantCount > 0)
                {
                    prompt.AppendLine($"Expected participants: {ctx.ParticipantCount}");
                }
                
                // Participant types
                if (ctx.ParticipantTypes != null)
                {
                    BuildParticipantTypeContext(prompt, ctx.ParticipantTypes);
                }
                
                // Goals and constraints
                if (ctx.Goals?.Count > 0)
                {
                    prompt.AppendLine($"\nKey objectives:");
                    foreach (var goal in ctx.Goals)
                    {
                        prompt.AppendLine($"- {goal}");
                    }
                }
                if (ctx.Constraints?.Count > 0)
                {
                    prompt.AppendLine($"\nConstraints:");
                    foreach (var constraint in ctx.Constraints)
                    {
                        prompt.AppendLine($"- {constraint}");
                    }
                }
                
                // Tone
                if (!string.IsNullOrEmpty(ctx.Tone))
                {
                    prompt.AppendLine($"\nTone: {ctx.Tone}");
                }
                
                // Activity type preferences
                if (ctx.IncludeActivityTypes?.Count > 0)
                {
                    prompt.AppendLine($"\nPrefer these activity types: {string.Join(", ", ctx.IncludeActivityTypes)}");
                }
                if (ctx.ExcludeActivityTypes?.Count > 0)
                {
                    prompt.AppendLine($"\nAvoid these activity types: {string.Join(", ", ctx.ExcludeActivityTypes)}");
                }
                
                // Context documents (with PII sanitization)
                if (ctx.ContextDocuments != null)
                {
                    BuildContextDocuments(prompt, ctx.ContextDocuments);
                }
            }
            else if (!string.IsNullOrEmpty(request.Context))
            {
                // Legacy context path
                var sanitizedContext = PiiSanitizer.Sanitize(request.Context, 500);
                if (PiiSanitizer.ContainsPii(request.Context))
                {
                    _logger.LogWarning("PII detected in legacy context and sanitized for session generation");
                }
                prompt.AppendLine($"Context: {sanitizedContext}");
            }
            
            prompt.AppendLine("\nReturn ONLY valid JSON array, no explanatory text.");
            
            return prompt.ToString();
        }

        private int CalculateActivityCount(int durationMinutes)
        {
            // Rough guide: 15-20 mins per activity
            if (durationMinutes <= 45) return 3;
            if (durationMinutes <= 75) return 4;
            if (durationMinutes <= 105) return 5;
            if (durationMinutes <= 135) return 6;
            return 7;
        }

        private void BuildParticipantTypeContext(StringBuilder prompt, ParticipantTypesDto participantTypes)
        {
            prompt.AppendLine("\nParticipants:");
            
            if (!string.IsNullOrEmpty(participantTypes.Primary))
            {
                prompt.AppendLine($"- Primary audience: {participantTypes.Primary}");
                
                // Add tone/terminology suggestions based on audience
                switch (participantTypes.Primary.ToLower())
                {
                    case "technical":
                        prompt.AppendLine("  → Use technical terminology, focus on implementation details, code quality, architecture.");
                        break;
                    case "business":
                        prompt.AppendLine("  → Use business terminology, focus on ROI, impact, strategy, outcomes.");
                        break;
                    case "managers":
                        prompt.AppendLine("  → Focus on team dynamics, process improvements, efficiency, leadership.");
                        break;
                    case "leaders":
                        prompt.AppendLine("  → Focus on strategic vision, organizational impact, change management.");
                        break;
                }
            }
            
            if (participantTypes.Breakdown?.Count > 0)
            {
                prompt.AppendLine($"- Breakdown:");
                foreach (var kvp in participantTypes.Breakdown)
                {
                    prompt.AppendLine($"  - {kvp.Key}: {kvp.Value}%");
                }
            }
            
            if (participantTypes.ExperienceLevels?.Count > 0)
            {
                prompt.AppendLine($"- Experience levels:");
                foreach (var kvp in participantTypes.ExperienceLevels)
                {
                    prompt.AppendLine($"  - {kvp.Key}: {kvp.Value}%");
                }
            }
            
            if (participantTypes.CustomRoles?.Count > 0)
            {
                prompt.AppendLine($"- Custom roles: {string.Join(", ", participantTypes.CustomRoles)}");
            }
        }

        private void BuildContextDocuments(StringBuilder prompt, ContextDocumentsDto documents)
        {
            bool hasAnyDocument = false;
            
            // Sprint backlog
            if (documents.SprintBacklog?.Provided == true)
            {
                hasAnyDocument = true;
                prompt.AppendLine("\n📋 Sprint Backlog Context:");
                var summary = PiiSanitizer.Sanitize(documents.SprintBacklog.Summary, 500);
                prompt.AppendLine(summary);
                if (documents.SprintBacklog.KeyItems?.Count > 0)
                {
                    prompt.AppendLine("Key items:");
                    foreach (var item in documents.SprintBacklog.KeyItems)
                    {
                        var sanitizedItem = PiiSanitizer.Sanitize(item, 200);
                        prompt.AppendLine($"- {sanitizedItem}");
                    }
                }
                prompt.AppendLine("→ Generate activities that reference specific backlog items or stories.");
            }
            
            // Incident report
            if (documents.IncidentReport?.Provided == true)
            {
                hasAnyDocument = true;
                prompt.AppendLine("\n🚨 Incident Report Context:");
                var summary = PiiSanitizer.Sanitize(documents.IncidentReport.Summary, 500);
                prompt.AppendLine(summary);
                if (!string.IsNullOrEmpty(documents.IncidentReport.Severity))
                {
                    prompt.AppendLine($"Severity: {documents.IncidentReport.Severity}");
                }
                if (documents.IncidentReport.ImpactedSystems?.Count > 0)
                {
                    prompt.AppendLine($"Impacted systems: {string.Join(", ", documents.IncidentReport.ImpactedSystems)}");
                }
                if (documents.IncidentReport.DurationMinutes > 0)
                {
                    prompt.AppendLine($"Incident duration: {documents.IncidentReport.DurationMinutes} minutes");
                }
                prompt.AppendLine("→ Generate postmortem-style activities focusing on root cause analysis and improvement.");
            }
            
            // Product documentation
            if (documents.ProductDocumentation?.Provided == true)
            {
                hasAnyDocument = true;
                prompt.AppendLine("\n📖 Product Documentation Context:");
                var summary = PiiSanitizer.Sanitize(documents.ProductDocumentation.Summary, 500);
                prompt.AppendLine(summary);
                if (documents.ProductDocumentation.Features?.Count > 0)
                {
                    prompt.AppendLine("Key features:");
                    foreach (var feature in documents.ProductDocumentation.Features)
                    {
                        var sanitized = PiiSanitizer.Sanitize(feature, 200);
                        prompt.AppendLine($"- {sanitized}");
                    }
                }
                prompt.AppendLine("→ Reference specific features in your questions.");
            }
            
            // Custom documents
            if (documents.CustomDocuments?.Count > 0)
            {
                foreach (var doc in documents.CustomDocuments)
                {
                    if (doc.Provided)
                    {
                        hasAnyDocument = true;
                        prompt.AppendLine($"\n📄 {doc.Type ?? "Custom Document"}:");
                        var summary = PiiSanitizer.Sanitize(doc.Summary, 500);
                        prompt.AppendLine(summary);
                        if (doc.KeyPoints?.Count > 0)
                        {
                            prompt.AppendLine("Key points:");
                            foreach (var point in doc.KeyPoints)
                            {
                                var sanitized = PiiSanitizer.Sanitize(point, 200);
                                prompt.AppendLine($"- {sanitized}");
                            }
                        }
                    }
                }
            }
            
            if (hasAnyDocument)
            {
                prompt.AppendLine("\n⚠️ IMPORTANT: Reference the above context documents in your generated activities to make them specific and relevant.");
            }
        }

        private string BuildSystemPrompt()
        {
            return @"You are an expert workshop facilitator and instructional designer. Generate a complete, valid JSON array of workshop activities.

IMPORTANT: Only use these 4 activity types: Poll, WordCloud, Rating, GeneralFeedback

For each activity, return:
- type: Poll | WordCloud | Rating | GeneralFeedback
- title: Clear, engaging title (3-50 chars)
- prompt: The question or instruction for participants (10-500 chars)
- durationMinutes: Recommended time (5-30 minutes)
- config: Activity-specific configuration object

Activity Guidelines:
- Poll: Use for quick consensus, voting, or decision-making. Config should include 'options' array with 2-6 choices.
- WordCloud: Use for brainstorming or sentiment capture. Config should set 'maxWords' (1-3), 'allowMultipleSubmissions'.
- Rating: Use for satisfaction or confidence checks. Config should set 'scale' (5 or 10), 'minLabel', 'maxLabel', 'allowComments'.
- GeneralFeedback: Use for open-ended input. Config can include 'categories' for organization.

Return ONLY a valid JSON array. No markdown, no explanation.";
        }

        private IReadOnlyList<AgendaActivityResponse> ParseActivitiesJson(string jsonText)
        {
            try
            {
                var list = new List<AgendaActivityResponse>();
                using var doc = JsonDocument.Parse(jsonText);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return list;

                int order = 1;
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var typeStr = item.TryGetProperty("type", out var t) ? t.GetString() ?? "GeneralFeedback" : "GeneralFeedback";
                    if (!Enum.TryParse<ActivityType>(typeStr, true, out var at)) at = ActivityType.GeneralFeedback;

                    var title = item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "Untitled" : "Untitled";
                    var prompt = item.TryGetProperty("prompt", out var promptEl) ? promptEl.GetString() : null;
                    int? duration = null;
                    if (item.TryGetProperty("durationMinutes", out var durEl) && durEl.ValueKind == JsonValueKind.Number)
                    {
                        duration = durEl.GetInt32();
                    }
                    string? config = null;
                    if (item.TryGetProperty("config", out var cfg) && cfg.ValueKind != JsonValueKind.Null)
                    {
                        config = cfg.GetRawText();
                    }

                    list.Add(new AgendaActivityResponse(Guid.NewGuid(), order++, at, title, prompt, config, TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending, null, null, duration));
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse activities JSON");
                return Array.Empty<AgendaActivityResponse>();
            }
        }
    }
}
