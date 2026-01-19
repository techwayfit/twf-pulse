using System.Text.Json;
using System.Text.RegularExpressions;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Services;

public sealed class GeneralFeedbackDashboardService : IGeneralFeedbackDashboardService
{
    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;
    private readonly IActivityRepository _activities;

    public GeneralFeedbackDashboardService(
        IResponseRepository responses,
        IParticipantRepository participants,
        IActivityRepository activities)
    {
        _responses = responses;
        _participants = participants;
        _activities = activities;
    }

    public async Task<GeneralFeedbackDashboardResponse> GetGeneralFeedbackDashboardAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        if (activityId == Guid.Empty)
        {
            throw new ArgumentException("Activity id is required.", nameof(activityId));
        }

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity == null)
        {
            throw new ArgumentException("Activity not found.", nameof(activityId));
        }

        if (activity.Type != Domain.Enums.ActivityType.GeneralFeedback)
        {
            throw new ArgumentException("Activity is not a GeneralFeedback type.", nameof(activityId));
        }

        var responses = await _responses.GetByActivityAsync(activityId, cancellationToken);
        var filteredResponses = ApplyFilters(responses, filters);

        var participants = await _participants.GetBySessionAsync(sessionId, cancellationToken);
        var respondedParticipants = filteredResponses
            .Select(response => response.ParticipantId)
            .Distinct()
            .Count();

        var lastResponseAt = filteredResponses.Count == 0
            ? (DateTimeOffset?)null
            : filteredResponses.Max(response => response.CreatedAt);

        // Parse feedback items
        var feedbacks = ParseFeedbackResponses(filteredResponses);

        // Calculate average word count
        var averageWordCount = feedbacks.Count > 0 
            ? (int)feedbacks.Average(f => f.WordCount) 
            : 0;

        // Extract top keywords
        var topKeywords = ExtractTopKeywords(feedbacks, 10);

        return new GeneralFeedbackDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            filteredResponses.Count,
            participants.Count,
            respondedParticipants,
            feedbacks,
            topKeywords,
            lastResponseAt,
            averageWordCount);
    }

    private static List<Domain.Entities.Response> ApplyFilters(
        IReadOnlyList<Domain.Entities.Response> responses,
        IReadOnlyDictionary<string, string?> filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return responses.ToList();
        }

        return responses
            .Where(response => MatchesFilters(response.Dimensions, filters))
            .ToList();
    }

    private static bool MatchesFilters(
        IReadOnlyDictionary<string, string?> dimensions,
        IReadOnlyDictionary<string, string?> filters)
    {
        foreach (var filter in filters)
        {
            if (string.IsNullOrEmpty(filter.Value))
            {
                continue;
            }

            if (!dimensions.TryGetValue(filter.Key, out var value) 
                || !string.Equals(value, filter.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static List<FeedbackItem> ParseFeedbackResponses(IReadOnlyList<Domain.Entities.Response> responses)
    {
        var result = new List<FeedbackItem>();

        foreach (var response in responses)
        {
            try
            {
                using var document = JsonDocument.Parse(response.Payload);
                var root = document.RootElement;

                string? content = null;
                if (root.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
                {
                    content = contentElement.GetString();
                }
                else if (root.TryGetProperty("feedback", out var feedbackElement) && feedbackElement.ValueKind == JsonValueKind.String)
                {
                    content = feedbackElement.GetString();
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                string? category = null;
                if (root.TryGetProperty("category", out var categoryElement) && categoryElement.ValueKind == JsonValueKind.String)
                {
                    category = categoryElement.GetString();
                }

                var isAnonymous = false;
                if (root.TryGetProperty("isAnonymous", out var isAnonymousElement) && isAnonymousElement.ValueKind == JsonValueKind.True)
                {
                    isAnonymous = true;
                }

                var wordCount = CountWords(content);

                result.Add(new FeedbackItem(
                    content,
                    wordCount,
                    category,
                    response.CreatedAt,
                    isAnonymous));
            }
            catch (JsonException)
            {
                // Skip malformed responses
            }
        }

        return result.OrderByDescending(f => f.CreatedAt).ToList();
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return Regex.Split(text, @"\s+")
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Count();
    }

    private static List<string> ExtractTopKeywords(IReadOnlyList<FeedbackItem> feedbacks, int topN)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
            "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
            "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
            "or", "an", "will", "my", "one", "all", "would", "there", "their",
            "what", "so", "up", "out", "if", "about", "who", "get", "which", "go", "me"
        };

        var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var feedback in feedbacks)
        {
            var words = Regex.Split(feedback.Content, @"\W+")
                .Where(word => !string.IsNullOrWhiteSpace(word) 
                    && word.Length > 2 
                    && !stopWords.Contains(word));

            foreach (var word in words)
            {
                var lowerWord = word.ToLowerInvariant();
                if (wordFrequency.ContainsKey(lowerWord))
                {
                    wordFrequency[lowerWord]++;
                }
                else
                {
                    wordFrequency[lowerWord] = 1;
                }
            }
        }

        return wordFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(topN)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
