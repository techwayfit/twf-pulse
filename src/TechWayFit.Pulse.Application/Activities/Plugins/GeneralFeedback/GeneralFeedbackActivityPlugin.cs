using System.Text.Json;
using System.Text.RegularExpressions;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.GeneralFeedback;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.GeneralFeedback"/> activity type.
/// </summary>
public sealed class GeneralFeedbackActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.GeneralFeedback;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "General Feedback",
        FaIconClass: "fa-solid fa-message",
        BadgeColorHex: "#20c997",
        BadgeTextColorHex: "#ffffff",
        AcceptsResponses: true,
        ConfigType: typeof(GeneralFeedbackConfig),
        ResponsePayloadType: typeof(GeneralFeedbackResponse));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"maxLength":1000,"minLength":10,"placeholder":"Share your thoughts, problems, or suggestions...","allowAnonymous":true,"categories":[],"maxResponsesPerParticipant":5}""";

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        if (string.IsNullOrWhiteSpace(config))
            return GetDefaultConfig();

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            var max = defaults.GeneralFeedbackMaxResponsesPerParticipant;

            if (root.TryGetProperty("maxResponsesPerParticipant", out var maxElem)
                && maxElem.TryGetInt32(out var stored)
                && stored <= max)
            {
                return config;
            }

            using var ms = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "maxResponsesPerParticipant")
                    writer.WriteNumber("maxResponsesPerParticipant", max);
                else
                    prop.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        catch (JsonException)
        {
            return GetDefaultConfig();
        }
    }

    public bool ValidateConfig(string? config, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();

        if (string.IsNullOrWhiteSpace(config))
        {
            list.Add("GeneralFeedback config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (root.TryGetProperty("minLength", out var minElem)
                && minElem.TryGetInt32(out var min)
                && min < 1)
            {
                list.Add("minLength must be at least 1.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"GeneralFeedback config is not valid JSON: {ex.Message}");
        }

        errors = list;
        return list.Count == 0;
    }

    // ── Response contract ─────────────────────────────────────────────────────

    public bool AcceptsResponses => Metadata.AcceptsResponses;

    public bool ValidateResponsePayload(string payload, out string? error)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "Response payload is required.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && (root.TryGetProperty("content", out _) || root.TryGetProperty("feedback", out _)))
            {
                error = null;
                return true;
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                error = null;
                return true;
            }

            error = "GeneralFeedback response payload must be an object with a 'content' or 'feedback' property.";
            return false;
        }
        catch (JsonException ex)
        {
            error = $"Response payload is not valid JSON: {ex.Message}";
            return false;
        }
    }

    // ── AI participation ──────────────────────────────────────────────────────

    public bool IncludeInAiSummary => true;

    public bool CanBeAiGenerated => true;

    // ── Dashboard data ────────────────────────────────────────────────────────

    public async Task<IActivityDashboardData> GetDashboardDataAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        IActivityDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session id is required.", nameof(sessionId));

        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity id is required.", nameof(activityId));

        var activity = await dataContext.GetActivityAsync(activityId, cancellationToken);
        if (activity is null)
            throw new ArgumentException("Activity not found.", nameof(activityId));

        var allResponses = await dataContext.GetResponsesAsync(activityId, cancellationToken);
        var filtered     = ApplyFilters(allResponses, filters);
        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);

        var respondedParticipants = filtered.Select(r => r.ParticipantId).Distinct().Count();
        var lastResponseAt        = filtered.Count == 0 ? (DateTimeOffset?)null : filtered.Max(r => r.CreatedAt);

        var feedbacks        = ParseFeedbackResponses(filtered);
        var averageWordCount = feedbacks.Count > 0 ? (int)feedbacks.Average(f => f.WordCount) : 0;
        var topKeywords      = ExtractTopKeywords(feedbacks, 10);

        var result = new GeneralFeedbackDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            filtered.Count,
            participants.Count,
            respondedParticipants,
            feedbacks,
            topKeywords,
            lastResponseAt,
            averageWordCount);

        return new GeneralFeedbackDashboardData(result);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static List<Domain.Entities.Response> ApplyFilters(
        IReadOnlyList<Domain.Entities.Response> responses,
        IReadOnlyDictionary<string, string?> filters)
    {
        if (filters is null || filters.Count == 0)
            return responses.ToList();

        return responses
            .Where(r => MatchesFilters(r.Dimensions, filters))
            .ToList();
    }

    private static bool MatchesFilters(
        IReadOnlyDictionary<string, string?> dimensions,
        IReadOnlyDictionary<string, string?> filters)
    {
        foreach (var filter in filters)
        {
            if (string.IsNullOrEmpty(filter.Value))
                continue;

            if (!dimensions.TryGetValue(filter.Key, out var value)
                || !string.Equals(value, filter.Value, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static List<FeedbackItem> ParseFeedbackResponses(
        IReadOnlyList<Domain.Entities.Response> responses)
    {
        var result = new List<FeedbackItem>();

        foreach (var response in responses)
        {
            try
            {
                using var document = JsonDocument.Parse(response.Payload);
                var root = document.RootElement;

                string? content = null;
                if (root.TryGetProperty("content", out var contentElement)
                    && contentElement.ValueKind == JsonValueKind.String)
                {
                    content = contentElement.GetString();
                }
                else if (root.TryGetProperty("feedback", out var feedbackElement)
                    && feedbackElement.ValueKind == JsonValueKind.String)
                {
                    content = feedbackElement.GetString();
                }

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                string? category = null;
                if (root.TryGetProperty("category", out var categoryElement)
                    && categoryElement.ValueKind == JsonValueKind.String)
                {
                    category = categoryElement.GetString();
                }

                var isAnonymous = root.TryGetProperty("isAnonymous", out var isAnonymousElement)
                    && isAnonymousElement.ValueKind == JsonValueKind.True;

                var wordCount = CountWords(content);

                result.Add(new FeedbackItem(content, wordCount, category, response.CreatedAt, isAnonymous));
            }
            catch (JsonException) { }
        }

        return result.OrderByDescending(f => f.CreatedAt).ToList();
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

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
                wordFrequency[lowerWord] = wordFrequency.GetValueOrDefault(lowerWord, 0) + 1;
            }
        }

        return wordFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(topN)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
