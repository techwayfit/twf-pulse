using System.Text.Json;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Services;

public sealed class RatingDashboardService : IRatingDashboardService
{
    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;
    private readonly IActivityRepository _activities;

    public RatingDashboardService(
        IResponseRepository responses,
        IParticipantRepository participants,
        IActivityRepository activities)
    {
        _responses = responses;
        _participants = participants;
        _activities = activities;
    }

    public async Task<RatingDashboardResponse> GetRatingDashboardAsync(
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

        if (activity.Type != Domain.Enums.ActivityType.Rating)
        {
            throw new ArgumentException("Activity is not a Rating type.", nameof(activityId));
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

        // Parse ratings and comments from responses
        var ratingData = ParseRatingResponses(filteredResponses);
        var ratings = ratingData.Select(r => r.Rating).ToList();
        var comments = ratingData
            .Where(r => !string.IsNullOrWhiteSpace(r.Comment))
            .Select(r => new RatingCommentItem(r.Rating, r.Comment!, r.CreatedAt))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        // Calculate statistics
        var averageRating = ratings.Count > 0 ? ratings.Average() : 0.0;
        var medianRating = CalculateMedian(ratings);
        var minRating = ratings.Count > 0 ? ratings.Min() : 0;
        var maxRating = ratings.Count > 0 ? ratings.Max() : 0;

        // Calculate distribution
        var distribution = ratings
            .GroupBy(r => r)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .OrderBy(x => x.Rating)
            .Select(x => new RatingDistributionItem(
                x.Rating,
                x.Count,
                ratings.Count > 0 ? (double)x.Count / ratings.Count * 100.0 : 0.0))
            .ToList();

        return new RatingDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            filteredResponses.Count,
            participants.Count,
            respondedParticipants,
            averageRating,
            medianRating,
            minRating,
            maxRating,
            distribution,
            comments,
            lastResponseAt);
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

    private static List<RatingResponseData> ParseRatingResponses(IReadOnlyList<Domain.Entities.Response> responses)
    {
        var result = new List<RatingResponseData>();

        foreach (var response in responses)
        {
            try
            {
                using var document = JsonDocument.Parse(response.Payload);
                var root = document.RootElement;

                var rating = 0;
                if (root.TryGetProperty("rating", out var ratingElement) && ratingElement.ValueKind == JsonValueKind.Number)
                {
                    rating = ratingElement.GetInt32();
                }

                string? comment = null;
                if (root.TryGetProperty("comment", out var commentElement) && commentElement.ValueKind == JsonValueKind.String)
                {
                    comment = commentElement.GetString();
                }

                if (rating > 0)
                {
                    result.Add(new RatingResponseData(rating, comment, response.CreatedAt));
                }
            }
            catch (JsonException)
            {
                // Skip malformed responses
            }
        }

        return result;
    }

    private static double CalculateMedian(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0.0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;

        if (sorted.Count % 2 == 0)
        {
            return (sorted[mid - 1] + sorted[mid]) / 2.0;
        }

        return sorted[mid];
    }

    private sealed record RatingResponseData(int Rating, string? Comment, DateTimeOffset CreatedAt);
}
