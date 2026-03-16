using System.Text.Json;
using System.Text.RegularExpressions;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.WordCloud;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.WordCloud"/> activity type.
/// </summary>
public sealed class WordCloudActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.WordCloud;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "Word Cloud",
        FaIconClass: "fa-solid fa-cloud",
        BadgeColorHex: "#198754",
        BadgeTextColorHex: "#ffffff",
        AcceptsResponses: true,
        ConfigType: typeof(WordCloudConfig),
        ResponsePayloadType: typeof(WordCloudResponse));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"maxWords":3,"minWordLength":3,"maxWordLength":50,"placeholder":"Enter a word or short phrase","maxSubmissionsPerParticipant":1,"stopWords":[],"caseSensitive":false}""";

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        if (string.IsNullOrWhiteSpace(config))
            return GetDefaultConfig();

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            var max = defaults.WordCloudMaxSubmissionsPerParticipant;

            if (root.TryGetProperty("maxSubmissionsPerParticipant", out var maxElem)
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
                if (prop.Name == "maxSubmissionsPerParticipant")
                    writer.WriteNumber("maxSubmissionsPerParticipant", max);
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
            list.Add("WordCloud config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (root.TryGetProperty("maxWords", out var maxWordsElem)
                && maxWordsElem.TryGetInt32(out var maxWords)
                && maxWords < 1)
            {
                list.Add("maxWords must be at least 1.");
            }

            if (root.TryGetProperty("minWordLength", out var minLen)
                && minLen.TryGetInt32(out var min)
                && min < 1)
            {
                list.Add("minWordLength must be at least 1.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"WordCloud config is not valid JSON: {ex.Message}");
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

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("text", out _))
            {
                error = null;
                return true;
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                error = null;
                return true;
            }

            error = "WordCloud response payload must be an object with a 'text' property or a plain string.";
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

        // Parse WordCloud configuration
        WordCloudConfig? config = null;
        if (!string.IsNullOrEmpty(activity.Config))
        {
            try
            {
                config = JsonSerializer.Deserialize<WordCloudConfig>(
                    activity.Config,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException) { }
        }
        config ??= new WordCloudConfig();

        var allResponses  = await dataContext.GetResponsesAsync(activityId, cancellationToken);
        var participants  = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);
        var filtered      = ApplyFilters(allResponses, filters);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var validResponses = new List<WordCloudResponse>();
        var responseParticipantIds = new HashSet<Guid>();

        foreach (var response in filtered)
        {
            try
            {
                var wc = JsonSerializer.Deserialize<WordCloudResponse>(response.Payload, jsonOptions);
                if (wc != null && !string.IsNullOrWhiteSpace(wc.Text))
                {
                    validResponses.Add(wc);
                    responseParticipantIds.Add(response.ParticipantId);
                }
            }
            catch (JsonException) { }
            catch (ArgumentException) { }
        }

        var wordFrequencies = ProcessWordFrequencies(validResponses, config);
        var lastResponseAt  = filtered.Count == 0 ? (DateTimeOffset?)null : filtered.Max(r => r.CreatedAt);
        var totalWords      = validResponses.SelectMany(r => ExtractWords(r.Text, config)).Count();
        var uniqueWords     = wordFrequencies.Count;

        var result = new WordCloudDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            validResponses.Count,
            participants.Count,
            responseParticipantIds.Count,
            wordFrequencies,
            lastResponseAt,
            totalWords,
            uniqueWords);

        return new WordCloudDashboardData(result);
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

    private static IReadOnlyList<WordCloudItem> ProcessWordFrequencies(
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

    private static IEnumerable<string> ExtractWords(string text, WordCloudConfig config)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var words = Regex.Split(text.Trim(), @"\s+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => Regex.Replace(w, @"[^\p{L}\p{Nd}'-]", ""))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Where(w => w.Length >= config.MinWordLength && w.Length <= config.MaxWordLength);

        foreach (var word in words)
        {
            var isStopWord = config.StopWords.Any(sw =>
                string.Equals(sw, word, config.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

            if (!isStopWord)
                yield return word;
        }
    }
}
