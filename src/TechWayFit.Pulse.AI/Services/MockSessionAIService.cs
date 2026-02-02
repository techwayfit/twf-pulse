using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.AI.Services
{
    public class MockSessionAIService : ISessionAIService
    {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "he", "in", "is", "it",
            "its", "of", "on", "that", "the", "to", "was", "will", "with", "we", "our", "your", "this", "these",
            "those", "what", "which", "who", "when", "where", "why", "how", "can", "could", "should", "would"
        };

        public Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(
            CreateSessionRequest request, 
            CancellationToken cancellationToken = default)
        {
            // Extract keywords from title, context, and goal
            var keywords = ExtractKeywords(request.Title, request.Context, request.Goal);
            
            // Generate activities based on keywords and session info
            var activities = GenerateActivities(request, keywords);

            return Task.FromResult<IReadOnlyList<AgendaActivityResponse>>(activities);
        }

        private List<string> ExtractKeywords(string title, string? context, string? goal)
        {
            var allText = $"{title} {context} {goal}";
            
            // Remove special characters and split into words
            var words = Regex.Replace(allText, @"[^\w\s]", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .Where(w => w.Length > 3 && !StopWords.Contains(w))
                .GroupBy(w => w.ToLowerInvariant())
                .OrderByDescending(g => g.Count())
                .Select(g => CapitalizeFirst(g.Key))
                .Take(10)
                .ToList();

            return words.Any() ? words : new List<string> { "Innovation", "Team", "Strategy" };
        }

        private List<AgendaActivityResponse> GenerateActivities(CreateSessionRequest request, List<string> keywords)
        {
            var activities = new List<AgendaActivityResponse>();
            var activitySequence = 1;

            // 1. Icebreaker Poll - Use first keyword or session theme
            var mainTheme = keywords.FirstOrDefault() ?? "Team";
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                $"{mainTheme} Icebreaker",
                $"What's your experience level with {mainTheme.ToLower()}?",
                GeneratePollOptions(new[] { "Beginner", "Intermediate", "Advanced", "Expert" }),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 3
            ));

            // 2. Initial Poll - Based on goal or context
            if (!string.IsNullOrWhiteSpace(request.Goal))
            {
                var goalKeywords = ExtractKeywords(request.Goal, null, null).Take(2).ToList();
                var pollQuestion = goalKeywords.Any() 
                    ? $"What's your biggest challenge with {string.Join(" and ", goalKeywords.Select(k => k.ToLower()))}?"
                    : "What's your biggest challenge right now?";
                
                activities.Add(new AgendaActivityResponse(
                    Guid.NewGuid(),
                    activitySequence++,
                    TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                    "Challenge Assessment",
                    pollQuestion,
                    GeneratePollOptions(keywords.Take(4)),
                    TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                    null, null, 4
                ));
            }

            // 3. Word Cloud - Capture key themes
            var wordCloudTopic = keywords.Take(2).Any() 
                ? string.Join(" and ", keywords.Take(2).Select(k => k.ToLower()))
                : "today's topic";
            
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud,
                "Key Themes",
                $"In one word, what comes to mind when you think about {wordCloudTopic}?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            // 4. General Feedback - Context-based question
            var feedbackTopic = !string.IsNullOrWhiteSpace(request.Context)
                ? ExtractKeywords(request.Context, null, null).FirstOrDefault() ?? "this topic"
                : keywords.FirstOrDefault() ?? "this topic";
            
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                "Ideas & Insights",
                $"Share one idea or insight about {feedbackTopic.ToLower()}",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 6
            ));

            // 5. Quadrant - Prioritization
            var quadrantTopic = keywords.Take(3).Any()
                ? string.Join(", ", keywords.Take(3).Select(k => k.ToLower()))
                : "ideas";
            
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Quadrant,
                "Impact vs Effort",
                $"Plot {quadrantTopic} based on Impact (Y-axis) and Effort (X-axis)",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 8
            ));

            // 6. Priority Poll - Action-oriented
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll,
                "Priority Vote",
                "Which area should we focus on first?",
                GeneratePollOptions(keywords.Take(4)),
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 4
            ));

            // 7. Word Cloud - Next Steps
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud,
                "Next Steps",
                "What's the ONE action we should take next?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            // 8. Final Feedback - Retrospective
            activities.Add(new AgendaActivityResponse(
                Guid.NewGuid(),
                activitySequence++,
                TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback,
                "Session Reflection",
                "What's your key takeaway from this session?",
                "{}",
                TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
                null, null, 5
            ));

            return activities;
        }

        private string GeneratePollOptions(IEnumerable<string> keywords)
        {
            var options = keywords.Take(4).ToList();
            
            // Ensure we have at least 3 options
            if (options.Count < 3)
            {
                var defaults = new[] { "Process", "People", "Technology", "Culture" };
                options.AddRange(defaults.Take(4 - options.Count));
            }

            var optionsArray = string.Join(", ", options.Select(o => $"\"{o}\""));
            return $"{{ \"options\": [{optionsArray}] }}";
        }

        private string CapitalizeFirst(string word)
        {
            if (string.IsNullOrEmpty(word)) return word;
            return char.ToUpper(word[0]) + word[1..].ToLower();
        }
    }
}
