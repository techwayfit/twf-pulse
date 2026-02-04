using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.AI.Services
{
    /// <summary>
    /// Intelligent session AI service for contextual question generation
    /// Uses NLP-inspired algorithms (TF-IDF, bigram extraction, domain detection) for keyword-based activity generation
    /// No external dependencies - pure C# implementation
    /// </summary>
    public class IntelligentSessionAIService : ISessionAIService
    {
        private readonly ILogger<IntelligentSessionAIService> _logger;

        // Common stop words to filter out
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "are", "as", "at", "be", "been", "by", "for", "from", "has", "have", "he",
            "her", "him", "his", "in", "is", "it", "its", "of", "on", "or", "that", "the", "their",
            "them", "they", "this", "to", "was", "were", "will", "with", "we", "our", "your", "you",
            "these", "those", "what", "which", "who", "when", "where", "why", "how", "can", "could",
            "should", "would", "may", "might", "must", "shall", "do", "does", "did", "done"
        };

        // Domain-specific keywords for workshop activities
        private static readonly Dictionary<string, string[]> DomainKeywordTemplates = new()
        {
            ["agile"] = new[] { "sprint planning", "retrospective", "daily standup", "backlog refinement" },
            ["devops"] = new[] { "CI/CD", "deployment", "automation", "infrastructure" },
            ["team"] = new[] { "collaboration", "communication", "trust", "accountability" },
            ["product"] = new[] { "roadmap", "features", "user stories", "requirements" },
            ["strategy"] = new[] { "goals", "objectives", "vision", "planning" },
            ["innovation"] = new[] { "ideas", "experimentation", "creativity", "solutions" },
            ["quality"] = new[] { "testing", "standards", "excellence", "improvement" },
            ["leadership"] = new[] { "vision", "decision-making", "empowerment", "coaching" }
        };

        public IntelligentSessionAIService(ILogger<IntelligentSessionAIService> logger)
        {
            _logger = logger;
        }

        public Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(
            CreateSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Intelligent Service: Generating activities for session '{Title}'", request.Title);

            // Extract and analyze keywords using NLP-inspired approach
            var keywords = ExtractKeywordsWithNLP(request.Title, request.Context, request.Goal);
            var domainContext = IdentifyDomainContext(keywords);
            
            _logger.LogDebug("Extracted {Count} keywords: {Keywords}", keywords.Count, string.Join(", ", keywords));
            _logger.LogDebug("Identified domain context: {Domain}", domainContext ?? "general");

            // Generate contextual activities
            var activities = GenerateContextualActivities(request, keywords, domainContext);

            _logger.LogInformation("Intelligent Service: Generated {Count} activities", activities.Count);

            return Task.FromResult<IReadOnlyList<AgendaActivityResponse>>(activities);
        }

        private List<string> ExtractKeywordsWithNLP(string title, string? context, string? goal)
        {
            var allText = $"{title} {context} {goal}";

            // Tokenize and clean text
            var tokens = Regex.Replace(allText, @"[^\w\s]", " ")
                .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 2 && !StopWords.Contains(w))
                .ToList();

            // Calculate TF (Term Frequency) with basic weighting
            var termFrequency = tokens
                .GroupBy(t => t)
                .ToDictionary(g => g.Key, g => g.Count());

            // Extract bigrams (two-word phrases) for better context
            var bigrams = new List<string>();
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var bigram = $"{tokens[i]} {tokens[i + 1]}";
                if (!StopWords.Contains(tokens[i]) && !StopWords.Contains(tokens[i + 1]))
                {
                    bigrams.Add(bigram);
                }
            }

            var bigramFrequency = bigrams
                .GroupBy(b => b)
                .Where(g => g.Count() > 1) // Only include repeated bigrams
                .ToDictionary(g => g.Key, g => g.Count() * 2); // Weight bigrams higher

            // Combine single words and bigrams, prioritize by frequency
            var rankedKeywords = termFrequency
                .Union(bigramFrequency)
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key.Length) // Prefer shorter terms if frequency is equal
                .Select(kv => CapitalizeKeyword(kv.Key))
                .Distinct()
                .Take(12)
                .ToList();

            // Fallback if no keywords found
            return rankedKeywords.Any() ? rankedKeywords : new List<string> { "Team", "Innovation", "Strategy" };
        }

        private string? IdentifyDomainContext(List<string> keywords)
        {
            // Match keywords against domain templates to identify context
            var keywordsLower = keywords.Select(k => k.ToLowerInvariant()).ToHashSet();

            foreach (var domain in DomainKeywordTemplates)
            {
                var matches = domain.Value.Count(template =>
                    keywordsLower.Any(k => k.Contains(template.ToLowerInvariant()) || template.ToLowerInvariant().Contains(k)));

                if (matches > 0 || keywordsLower.Contains(domain.Key))
                {
                    return domain.Key;
                }
            }

            return null; // General domain
        }

        private List<AgendaActivityResponse> GenerateContextualActivities(
            CreateSessionRequest request,
            List<string> keywords,
            string? domainContext)
        {
            var activities = new List<AgendaActivityResponse>();
            var activitySequence = 1;

            // Get domain-specific keywords if available
            var domainKeywords = domainContext != null && DomainKeywordTemplates.ContainsKey(domainContext)
                ? DomainKeywordTemplates[domainContext]
                : Array.Empty<string>();

            var primaryKeyword = keywords.FirstOrDefault() ?? "Team";
            var secondaryKeywords = keywords.Skip(1).Take(3).ToList();

            // 1. Warm-up Poll - Experience level
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Experience Check-in",
                $"How would you rate your current experience with {primaryKeyword.ToLower()}?",
                GeneratePollOptions(new[] { "Just starting", "Some experience", "Quite experienced", "Expert level" }),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 3
            ));

            // 2. Context Poll - Current state
            if (!string.IsNullOrWhiteSpace(request.Context))
            {
                var contextKeywords = ExtractKeywordsWithNLP(request.Context, null, null).Take(3).ToList();
                var pollQuestion = contextKeywords.Any()
                    ? $"Which of these areas needs the most attention?"
                    : "What's your top priority right now?";

                var pollOptions = contextKeywords.Any() ? contextKeywords : keywords.Take(4).ToList();

                activities.Add(new AgendaActivityResponse(
                    Guid.NewGuid(),
                    activitySequence++,
                    TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                    "Priority Assessment",
                    pollQuestion,
                    GeneratePollOptions(pollOptions),
                    TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                    null, null, 4
                ));
            }

            // 3. Initial Word Cloud - First impressions
            var wordCloudContext = secondaryKeywords.Any()
                ? string.Join(" or ", secondaryKeywords.Take(2).Select(k => k.ToLower()))
                : "today's topic";

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud,
                "First Impressions",
                $"What word best describes your current thinking about {wordCloudContext}?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 4
            ));

            // 4. Challenge Feedback - Pain points
            var challengeTopic = !string.IsNullOrWhiteSpace(request.Goal)
                ? ExtractKeywordsWithNLP(request.Goal, null, null).FirstOrDefault() ?? primaryKeyword
                : primaryKeyword;

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                "Challenge Identification",
                $"What's the biggest challenge you face with {challengeTopic.ToLower()}?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            // 5. Solution Poll - Approaches
            var solutionOptions = domainKeywords.Any()
                ? domainKeywords.Take(4).Select(CapitalizeKeyword).ToArray()
                : keywords.Take(4).ToArray();

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Solution Approach",
                "Which approach resonates most with you?",
                GeneratePollOptions(solutionOptions),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 4
            ));

            // 6. Idea Generation - Open feedback
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                "Idea Generation",
                $"Share one innovative idea for improving {primaryKeyword.ToLower()}",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 6
            ));

            // 7. Quadrant - Prioritization
            var quadrantItems = keywords.Take(3).Any()
                ? $"your ideas about {string.Join(", ", keywords.Take(3).Select(k => k.ToLower()))}"
                : "proposed solutions";

            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Quadrant,
                "Impact vs Effort Matrix",
                $"Plot {quadrantItems} on Impact (Y-axis) and Effort (X-axis)",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 8
            ));

            // 8. Action Word Cloud - Next steps
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud,
                "Action Planning",
                "What's the ONE thing we should do first?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            // 9. Commitment Poll - Action commitment
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Commitment Check",
                "How committed are you to taking action?",
                GeneratePollOptions(new[] { "Fully committed", "Mostly committed", "Somewhat committed", "Need more clarity" }),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 3
            ));

            // 10. Retrospective Feedback - Key takeaways
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                "Session Takeaway",
                "What's your biggest insight or learning from this session?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            return activities;
        }

        private string GeneratePollOptions(IEnumerable<string> keywords)
        {
            var options = keywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(CapitalizeKeyword)
                .Distinct()
                .Take(4)
                .ToList();

            // Ensure minimum 3 options
            if (options.Count < 3)
            {
                var defaults = new[] { "Process improvement", "Team collaboration", "Technical excellence", "Strategic alignment" };
                options.AddRange(defaults.Take(4 - options.Count));
            }

            var optionsArray = string.Join(", ", options.Select(o => $"\"{o}\""));
            return $"{{ \"options\": [{optionsArray}] }}";
        }

        private string CapitalizeKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return keyword;

            // Handle multi-word phrases
            var words = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Select(w => char.ToUpper(w[0]) + w[1..].ToLower()));
        }

        public Task<IReadOnlyList<AgendaActivityResponse>> GenerateAndAddActivitiesToSessionAsync(
            Session session,
            string? additionalContext,
            string? workshopType,
            int targetActivityCount,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Intelligent Service: Generating activities for existing session {SessionId}", session.Id);

            // Build a request from session data
            var request = new CreateSessionRequest
            {
                Title = session.Title,
                Goal = session.Goal,
                Context = additionalContext ?? session.Context,
                GenerationContext = new SessionGenerationContextDto
                {
                    WorkshopType = workshopType ?? "general",
                    DurationMinutes = targetActivityCount * 5 // Approximate duration based on activity count
                }
            };

            // Use existing generation logic
            return GenerateSessionActivitiesAsync(request, cancellationToken);
        }
    }
}
