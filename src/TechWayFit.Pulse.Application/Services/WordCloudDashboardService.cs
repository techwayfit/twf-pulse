using System.Text.Json;
using System.Text.RegularExpressions;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Services;

public sealed class WordCloudDashboardService : IWordCloudDashboardService
{
    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;
    private readonly IActivityRepository _activities;

    public WordCloudDashboardService(
        IResponseRepository responses,
        IParticipantRepository participants,
        IActivityRepository activities)
    {
        _responses = responses;
        _participants = participants;
        _activities = activities;
    }

    public async Task<WordCloudDashboardResponse> GetWordCloudDashboardAsync(
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

        if (activity.Type != Domain.Enums.ActivityType.WordCloud)
        {
            throw new ArgumentException("Activity is not a WordCloud type.", nameof(activityId));
        }

        // Parse WordCloud configuration
        WordCloudConfig? config = null;
        if (!string.IsNullOrEmpty(activity.Config))
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                config = JsonSerializer.Deserialize<WordCloudConfig>(activity.Config, options);
            }
            catch (JsonException)
            {
                // Ignore config parsing errors and use defaults
            }
        }

        config ??= new WordCloudConfig(); // Use defaults if parsing failed

        // Get all responses for this activity
        var responses = await _responses.GetByActivityAsync(activityId, cancellationToken);
        var participants = await _participants.GetBySessionAsync(sessionId, cancellationToken);

        var totalParticipants = participants.Count;
        var validResponses = new List<WordCloudResponse>();
        var responseParticipantIds = new HashSet<Guid>();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Parse and validate responses
        foreach (var response in responses)
        {
            try
            {
                var wordCloudResponse = JsonSerializer.Deserialize<WordCloudResponse>(response.Payload, jsonOptions);
                if (wordCloudResponse != null && !string.IsNullOrWhiteSpace(wordCloudResponse.Text))
                {
                    validResponses.Add(wordCloudResponse);
                    responseParticipantIds.Add(response.ParticipantId);
                }
            }
            catch (JsonException)
            {
                // Skip invalid JSON responses
            }
            catch (ArgumentException)
            {
                // Skip responses with invalid data (e.g., empty text)
            }
        }

        // Process words and calculate frequencies
        var wordFrequencies = ProcessWordFrequencies(validResponses, config);
        var lastResponseAt = responses.Any() ? responses.Max(r => r.CreatedAt) : (DateTimeOffset?)null;
        var totalWords = validResponses.SelectMany(r => ExtractWords(r.Text, config)).Count();
        var uniqueWords = wordFrequencies.Count;

        return new WordCloudDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            validResponses.Count,
            totalParticipants,
            responseParticipantIds.Count,
            wordFrequencies,
            lastResponseAt,
            totalWords,
            uniqueWords);
    }

    private IReadOnlyList<WordCloudItem> ProcessWordFrequencies(
        IReadOnlyList<WordCloudResponse> responses, 
        WordCloudConfig config)
    {
        var wordCounts = new Dictionary<string, int>(
            config.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (var response in responses)
        {
            var words = ExtractWords(response.Text, config);
            foreach (var word in words)
            {
                var key = config.CaseSensitive ? word : word.ToLowerInvariant();
                wordCounts[key] = wordCounts.GetValueOrDefault(key, 0) + 1;
            }
        }

        return wordCounts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .Select(kvp => new WordCloudItem(kvp.Key, kvp.Value))
            .ToList();
    }

    private IEnumerable<string> ExtractWords(string text, WordCloudConfig config)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        // Split text into words (handles various whitespace and punctuation)
        var words = Regex.Split(text.Trim(), @"\s+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => Regex.Replace(w, @"[^\p{L}\p{Nd}'-]", "")) // Keep letters, numbers, hyphens, apostrophes
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Where(w => w.Length >= config.MinWordLength && w.Length <= config.MaxWordLength);

        foreach (var word in words)
        {
            // Check if word is in stop words list
            var isStopWord = config.StopWords.Any(sw => 
                string.Equals(sw, word, config.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
            
            if (!isStopWord)
            {
                yield return word;
            }
        }
    }
}