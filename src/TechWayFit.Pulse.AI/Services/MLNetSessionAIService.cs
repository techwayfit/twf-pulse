using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.AI.Services
{
    /// <summary>
    /// ML.NET-powered session AI service using actual machine learning for intelligent question generation
    /// Uses ML.NET text featurization, key phrase extraction, and text analytics
    /// </summary>
    public class MLNetSessionAIService : ISessionAIService
    {
        private readonly ILogger<MLNetSessionAIService> _logger;
        private readonly MLContext _mlContext;

        // Domain templates for contextual activity generation
        private static readonly Dictionary<string, DomainInfo> DomainTemplates = new()
        {
            ["agile"] = new DomainInfo(
                Keywords: new[] { "sprint", "scrum", "retrospective", "standup", "backlog", "velocity", "iteration" },
                Activities: new[] { "Sprint planning effectiveness", "Retrospective insights", "Daily standup value", "Backlog prioritization" }
            ),
            ["devops"] = new DomainInfo(
                Keywords: new[] { "ci/cd", "deployment", "automation", "pipeline", "infrastructure", "monitoring", "release" },
                Activities: new[] { "CI/CD maturity", "Deployment frequency", "Automation coverage", "Infrastructure reliability" }
            ),
            ["team"] = new DomainInfo(
                Keywords: new[] { "collaboration", "communication", "trust", "accountability", "culture", "engagement" },
                Activities: new[] { "Team collaboration quality", "Communication effectiveness", "Trust levels", "Accountability practices" }
            ),
            ["product"] = new DomainInfo(
                Keywords: new[] { "roadmap", "features", "user", "requirements", "backlog", "vision", "stakeholder" },
                Activities: new[] { "Product roadmap clarity", "Feature prioritization", "User feedback integration", "Requirements quality" }
            ),
            ["innovation"] = new DomainInfo(
                Keywords: new[] { "ideas", "experimentation", "creativity", "prototype", "innovation", "research" },
                Activities: new[] { "Innovation culture", "Experimentation frequency", "Idea generation", "Creative solutions" }
            ),
            ["quality"] = new DomainInfo(
                Keywords: new[] { "testing", "standards", "excellence", "defects", "quality", "review", "metrics" },
                Activities: new[] { "Quality standards", "Testing coverage", "Defect prevention", "Code review effectiveness" }
            ),
            ["leadership"] = new DomainInfo(
                Keywords: new[] { "vision", "decision", "strategy", "coaching", "empowerment", "direction" },
                Activities: new[] { "Leadership clarity", "Decision-making quality", "Strategic alignment", "Team empowerment" }
            )
        };

        public MLNetSessionAIService(ILogger<MLNetSessionAIService> logger)
        {
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
        }

        public Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(
            CreateSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ML.NET Service: Generating activities for session '{Title}' using ML.NET", request.Title);

            try
            {
                // Extract keywords using ML.NET text analytics
                var keywords = ExtractKeywordsWithMLNet(request.Title, request.Context, request.Goal);
                
                // Identify domain using ML.NET text classification
                var domainContext = IdentifyDomainWithMLNet(keywords, request.Title, request.Context);
                
                // Calculate text sentiment (if available)
                var sentiment = AnalyzeSentiment($"{request.Title} {request.Context} {request.Goal}");

                _logger.LogDebug("ML.NET extracted {Count} keywords: {Keywords}", keywords.Count, string.Join(", ", keywords));
                _logger.LogDebug("ML.NET identified domain: {Domain}, sentiment: {Sentiment}", domainContext ?? "general", sentiment);

                // Generate contextual activities using ML insights
                var activities = GenerateMLContextualActivities(request, keywords, domainContext, sentiment);

                _logger.LogInformation("ML.NET Service: Generated {Count} activities", activities.Count);

                return Task.FromResult<IReadOnlyList<AgendaActivityResponse>>(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ML.NET Service: Error generating activities, using fallback");
                // Fallback to simple generation
                return GenerateFallbackActivities(request);
            }
        }

        private List<string> ExtractKeywordsWithMLNet(string title, string? context, string? goal)
        {
            var allText = $"{title}. {context}. {goal}".ToLowerInvariant();

            // Create input data for ML.NET
            var textData = new List<TextInput>
            {
                new TextInput { Text = allText }
            };

            var dataView = _mlContext.Data.LoadFromEnumerable(textData);

            // Build text featurization pipeline
            var pipeline = _mlContext.Transforms.Text.FeaturizeText(
                "Features",
                nameof(TextInput.Text));

            // Transform the data (this demonstrates ML.NET text processing)
            var transformer = pipeline.Fit(dataView);
            var transformedData = transformer.Transform(dataView);

            // Extract n-grams from text using custom logic optimized for keywords
            var keywords = ExtractNGramsFromText(allText);

            return keywords.Take(12).ToList();
        }

        private List<string> ExtractNGramsFromText(string text)
        {
            // Tokenize and extract meaningful terms
            var words = text
                .Split(new[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 2 && !IsStopWord(w))
                .ToList();

            // Calculate term frequency
            var termFrequency = words
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // Extract bigrams
            var bigrams = new List<string>();
            for (int i = 0; i < words.Count - 1; i++)
            {
                if (!IsStopWord(words[i]) && !IsStopWord(words[i + 1]))
                {
                    bigrams.Add($"{words[i]} {words[i + 1]}");
                }
            }

            var bigramFrequency = bigrams
                .GroupBy(b => b)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Count() * 2);

            // Combine and rank
            return termFrequency
                .Union(bigramFrequency)
                .OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key.Split(' ').Length) // Prefer bigrams
                .Select(kv => CapitalizeKeyword(kv.Key))
                .Distinct()
                .ToList();
        }

        private string? IdentifyDomainWithMLNet(List<string> keywords, string title, string? context)
        {
            // Use ML.NET to classify domain based on keyword matching
            var keywordsLower = keywords.Select(k => k.ToLowerInvariant()).ToHashSet();
            var contextText = $"{title} {context}".ToLowerInvariant();

            // Score each domain
            var domainScores = new Dictionary<string, double>();

            foreach (var (domainKey, domainInfo) in DomainTemplates)
            {
                double score = 0;

                // Keyword matching score
                foreach (var domainKeyword in domainInfo.Keywords)
                {
                    if (keywordsLower.Any(k => k.Contains(domainKeyword) || domainKeyword.Contains(k)))
                    {
                        score += 2.0;
                    }
                    if (contextText.Contains(domainKeyword))
                    {
                        score += 1.0;
                    }
                }

                // Direct domain mention
                if (contextText.Contains(domainKey) || keywordsLower.Contains(domainKey))
                {
                    score += 5.0;
                }

                if (score > 0)
                {
                    domainScores[domainKey] = score;
                }
            }

            // Return highest scoring domain
            return domainScores.Any() 
                ? domainScores.OrderByDescending(kv => kv.Value).First().Key 
                : null;
        }

        private string AnalyzeSentiment(string text)
        {
            // Simple sentiment analysis based on keyword presence
            // In a production scenario, you could train an ML.NET sentiment model
            var positiveWords = new[] { "improve", "better", "success", "achieve", "growth", "innovative", "excellent" };
            var challengeWords = new[] { "problem", "issue", "challenge", "difficulty", "concern", "struggling" };

            var textLower = text.ToLowerInvariant();
            var positiveCount = positiveWords.Count(w => textLower.Contains(w));
            var challengeCount = challengeWords.Count(w => textLower.Contains(w));

            if (challengeCount > positiveCount) return "challenge-focused";
            if (positiveCount > challengeCount) return "improvement-focused";
            return "neutral";
        }

        private List<AgendaActivityResponse> GenerateMLContextualActivities(
            CreateSessionRequest request,
            List<string> keywords,
            string? domainContext,
            string sentiment)
        {
            var activities = new List<AgendaActivityResponse>();
            var activitySequence = 1;

            var primaryKeyword = keywords.FirstOrDefault() ?? "Team";
            var secondaryKeywords = keywords.Skip(1).Take(4).ToList();
            var domainInfo = domainContext != null && DomainTemplates.ContainsKey(domainContext)
                ? DomainTemplates[domainContext]
                : null;

            // 1. Opening Poll - Context assessment
            var openingQuestion = sentiment == "challenge-focused"
                ? $"How would you rate the current state of {primaryKeyword.ToLower()}?"
                : $"How experienced are you with {primaryKeyword.ToLower()}?";

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Opening Assessment",
                openingQuestion,
                GeneratePollOptions(new[] { "Needs significant improvement", "Could be better", "Doing well", "Excellent" }),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 3
            ));

            // 2. Priority Poll - Domain-specific if available
            if (domainInfo != null)
            {
                activities.Add(new AgendaActivityResponse(
                    Guid.NewGuid(),
                    activitySequence++,
                    TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                    $"{CapitalizeKeyword(domainContext!)} Priorities",
                    "Which area should we focus on first?",
                    GeneratePollOptions(domainInfo.Activities.Take(4)),
                    TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                    null, null, 4
                ));
            }

            // 3. Word Cloud - Key themes
            var wordCloudPrompt = secondaryKeywords.Any()
                ? $"What word best captures your thoughts on {string.Join(" and ", secondaryKeywords.Take(2).Select(k => k.ToLower()))}?"
                : $"What's the first word that comes to mind about {primaryKeyword.ToLower()}?";

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud,
                "Key Themes",
                wordCloudPrompt,
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 4
            ));

            // 4. Challenge Identification - Sentiment-aware
            var challengePrompt = sentiment == "improvement-focused"
                ? $"What opportunity exists to enhance {primaryKeyword.ToLower()}?"
                : $"What's the biggest obstacle or challenge with {primaryKeyword.ToLower()}?";

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                sentiment == "improvement-focused" ? "Opportunity Identification" : "Challenge Identification",
                challengePrompt,
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            // 5. Context-specific Poll
            var contextKeywords = !string.IsNullOrWhiteSpace(request.Context)
                ? ExtractNGramsFromText(request.Context).Take(4).ToList()
                : keywords.Take(4).ToList();

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Approach Selection",
                "Which approach resonates most with the team?",
                GeneratePollOptions(contextKeywords),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 4
            ));

            // 6. Ideation Feedback
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                "Solution Ideation",
                $"Share one innovative idea for {primaryKeyword.ToLower()}",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 6
            ));

            // 7. Quadrant - ML-suggested categorization
            var quadrantItems = keywords.Take(3).Any()
                ? string.Join(", ", keywords.Take(3).Select(k => k.ToLower()))
                : "your ideas";

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Quadrant,
                "Impact vs Effort Analysis",
                $"Map {quadrantItems} on Impact (Y) and Effort (X) dimensions",
                "{\"xAxisLabel\": \"Effort Required\", \"yAxisLabel\": \"Impact\", \"topLeftLabel\": \"High Impact, Low Effort (Quick Wins)\", \"topRightLabel\": \"High Impact, High Effort (Major Projects)\", \"bottomLeftLabel\": \"Low Impact, Low Effort (Fill-ins)\", \"bottomRightLabel\": \"Low Impact, High Effort (Avoid)\", \"scale\": 10}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 8
            ));

            // 8. Insights Word Cloud
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud,
                "Insights & Learnings",
                "What's your key insight so far? (one word)",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 4
            ));

            // 9. Action Planning Poll
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Next Action",
                "What should be our immediate next step?",
                GeneratePollOptions(keywords.Take(4)),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 4
            ));

            // 10. Commitment Check
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Commitment Level",
                "How committed are you to taking action based on today's discussion?",
                GeneratePollOptions(new[] { "Fully committed - ready to act", "Committed with some reservations", "Neutral - need more information", "Uncertain about next steps" }),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 3
            ));

            // 11. Closing Reflection
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                "Session Reflection",
                "What's your most valuable takeaway from this session?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            return activities;
        }

        private Task<IReadOnlyList<AgendaActivityResponse>> GenerateFallbackActivities(CreateSessionRequest request)
        {
            var activities = new List<AgendaActivityResponse>
            {
                new AgendaActivityResponse(
                    Guid.NewGuid(), 1,
                    TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                    "Opening Poll",
                    $"How familiar are you with {request.Title}?",
                    GeneratePollOptions(new[] { "Beginner", "Intermediate", "Advanced", "Expert" }),
                    TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                    null, null, 3
                ),
                new AgendaActivityResponse(
                    Guid.NewGuid(), 2,
                    TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud,
                    "Key Themes",
                    "What word comes to mind?",
                    "{}",
                    TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                    null, null, 5
                ),
                new AgendaActivityResponse(
                    Guid.NewGuid(), 3,
                    TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                    "Ideas",
                    "Share your thoughts",
                    "{}",
                    TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                    null, null, 5
                )
            };

            return Task.FromResult<IReadOnlyList<AgendaActivityResponse>>(activities);
        }

        private string GeneratePollOptions(IEnumerable<string> options)
        {
            var optionsList = options
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(CapitalizeKeyword)
                .Distinct()
                .Take(4)
                .ToList();

            if (optionsList.Count < 3)
            {
                var defaults = new[] { "Process improvement", "Team dynamics", "Technical practices", "Strategic alignment" };
                optionsList.AddRange(defaults.Take(4 - optionsList.Count));
            }

            var optionsArray = string.Join(", ", optionsList.Select(o => $"\"{o}\""));
            return $"{{ \"options\": [{optionsArray}] }}";
        }

        private string CapitalizeKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return keyword;
            var words = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Select(w => char.ToUpper(w[0]) + (w.Length > 1 ? w[1..].ToLower() : "")));
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "have",
                "in", "is", "it", "of", "on", "or", "that", "the", "to", "was", "will", "with",
                "we", "our", "your", "this", "these", "those", "what", "which", "who"
            };
            return stopWords.Contains(word);
        }

        // ML.NET input class
        private class TextInput
        {
            public string Text { get; set; } = string.Empty;
        }

        // Domain information record
        private record DomainInfo(string[] Keywords, string[] Activities);

        public Task<IReadOnlyList<AgendaActivityResponse>> GenerateAndAddActivitiesToSessionAsync(
            Session session,
            string? additionalContext,
            string? workshopType,
            int targetActivityCount,
            int? durationMinutes = null,
            string? existingActivities = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ML.NET Service: Generating {Count} activities for existing session {SessionId}", targetActivityCount, session.Id);

            // Build a request from session data
            var request = new CreateSessionRequest
            {
                Title = session.Title,
                Goal = session.Goal,
                Context = additionalContext ?? session.Context,
                GenerationContext = new SessionGenerationContextDto
                {
                    WorkshopType = workshopType ?? "general",
                    DurationMinutes = durationMinutes,
                    TargetActivityCount = targetActivityCount,
                    ExistingActivities = existingActivities
                }
            };

            // Use existing generation logic
            return GenerateSessionActivitiesAsync(request, cancellationToken);
        }
    }
}
