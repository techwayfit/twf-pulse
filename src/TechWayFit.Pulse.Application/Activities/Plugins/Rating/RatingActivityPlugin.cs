using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Rating;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.Rating"/> activity type.
/// </summary>
public sealed class RatingActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.Rating;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "Rating",
        FaIconClass: "fa-solid fa-star",
        BadgeColorHex: "#ffc107",
        BadgeTextColorHex: "#000000",
        AcceptsResponses: true,
        ConfigType: typeof(RatingConfig),
        ResponsePayloadType: typeof(RatingResponse));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"scale":5,"minLabel":"1 - Low","maxLabel":"5 - High","allowComments":true,"displayType":"Buttons","maxResponsesPerParticipant":1}""";

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        if (string.IsNullOrWhiteSpace(config))
            return GetDefaultConfig();

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            var max = defaults.RatingMaxResponsesPerParticipant;

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
            list.Add("Rating config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (root.TryGetProperty("scale", out var scaleElem)
                && scaleElem.TryGetInt32(out var scale)
                && (scale < 2 || scale > 10))
            {
                list.Add("Rating scale must be between 2 and 10.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"Rating config is not valid JSON: {ex.Message}");
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

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("rating", out _))
            {
                error = null;
                return true;
            }

            if (root.ValueKind == JsonValueKind.Number)
            {
                error = null;
                return true;
            }

            error = "Rating response payload must be an object with a 'rating' property.";
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

        var ratingData = ParseRatingResponses(filtered);
        var ratings    = ratingData.Select(r => r.Rating).ToList();
        var comments   = ratingData
            .Where(r => !string.IsNullOrWhiteSpace(r.Comment))
            .Select(r => new RatingCommentItem(r.Rating, r.Comment!, r.CreatedAt))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var averageRating = ratings.Count > 0 ? ratings.Average() : 0.0;
        var medianRating  = CalculateMedian(ratings);
        var minRating     = ratings.Count > 0 ? ratings.Min() : 0;
        var maxRating     = ratings.Count > 0 ? ratings.Max() : 0;

        var distribution = ratings
            .GroupBy(r => r)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .OrderBy(x => x.Rating)
            .Select(x => new RatingDistributionItem(
                x.Rating,
                x.Count,
                ratings.Count > 0 ? (double)x.Count / ratings.Count * 100.0 : 0.0))
            .ToList();

        var response = new RatingDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            filtered.Count,
            participants.Count,
            respondedParticipants,
            averageRating,
            medianRating,
            minRating,
            maxRating,
            distribution,
            comments,
            lastResponseAt);

        return new RatingDashboardData(response);
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

    private static List<RatingResponseData> ParseRatingResponses(
        IReadOnlyList<Domain.Entities.Response> responses)
    {
        var result = new List<RatingResponseData>();

        foreach (var response in responses)
        {
            try
            {
                using var document = JsonDocument.Parse(response.Payload);
                var root = document.RootElement;

                var rating = 0;
                if (root.TryGetProperty("rating", out var ratingElement)
                    && ratingElement.ValueKind == JsonValueKind.Number)
                {
                    rating = ratingElement.GetInt32();
                }

                string? comment = null;
                if (root.TryGetProperty("comment", out var commentElement)
                    && commentElement.ValueKind == JsonValueKind.String)
                {
                    comment = commentElement.GetString();
                }

                if (rating > 0)
                    result.Add(new RatingResponseData(rating, comment, response.CreatedAt));
            }
            catch (JsonException) { }
        }

        return result;
    }

    private static double CalculateMedian(List<int> values)
    {
        if (values.Count == 0)
            return 0.0;

        var sorted = values.OrderBy(v => v).ToList();
        var mid    = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private sealed record RatingResponseData(int Rating, string? Comment, DateTimeOffset CreatedAt);
}
