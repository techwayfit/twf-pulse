using System.Text.Json;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "from", "has", "have", "i",
        "in", "is", "it", "of", "on", "or", "that", "the", "this", "to", "we", "with", "you"
    };

    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;
    private readonly IActivityRepository _activities;

    public DashboardService(
        IResponseRepository responses,
        IParticipantRepository participants,
        IActivityRepository activities)
    {
        _responses = responses;
        _participants = participants;
        _activities = activities;
    }

    public async Task<DashboardResponse> GetDashboardAsync(
        Guid sessionId,
        Guid? activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        var responses = activityId.HasValue
            ? await _responses.GetByActivityAsync(activityId.Value, cancellationToken)
            : await _responses.GetBySessionAsync(sessionId, cancellationToken);

        var filtered = ApplyFilters(responses, filters);

        var participants = await _participants.GetBySessionAsync(sessionId, cancellationToken);
        var respondedParticipants = filtered
            .Select(response => response.ParticipantId)
            .Distinct()
            .Count();

        var wordCloud = BuildWordCloud(filtered);
        var quadrantPoints = await BuildQuadrantPointsAsync(filtered, activityId, cancellationToken);

        return new DashboardResponse(
            sessionId,
            activityId,
            filtered.Count,
            participants.Count,
            respondedParticipants,
            wordCloud,
            quadrantPoints);
    }

    private static List<Response> ApplyFilters(
        IReadOnlyList<Response> responses,
        IReadOnlyDictionary<string, string?> filters)
    {
        if (filters.Count == 0)
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
            if (string.IsNullOrWhiteSpace(filter.Value))
            {
                continue;
            }

            if (!dimensions.TryGetValue(filter.Key, out var value))
            {
                return false;
            }

            if (!string.Equals(value, filter.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<WordCloudItem> BuildWordCloud(IReadOnlyList<Response> responses)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var response in responses)
        {
            var text = ExtractText(response.Payload);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (var token in Tokenize(text))
            {
                if (StopWords.Contains(token))
                {
                    continue;
                }

                counts[token] = counts.TryGetValue(token, out var count) ? count + 1 : 1;
            }
        }

        return counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => new WordCloudItem(pair.Key, pair.Value))
            .ToList();
    }

    private async Task<IReadOnlyList<QuadrantPoint>> BuildQuadrantPointsAsync(
        IReadOnlyList<Response> responses,
        Guid? activityId,
        CancellationToken cancellationToken)
    {
        if (!activityId.HasValue)
        {
            return ExtractQuadrantPoints(responses);
        }

        var activity = await _activities.GetByIdAsync(activityId.Value, cancellationToken);
        if (activity?.Type != Domain.Enums.ActivityType.Quadrant)
        {
            return Array.Empty<QuadrantPoint>();
        }

        return ExtractQuadrantPoints(responses);
    }

    private static IReadOnlyList<QuadrantPoint> ExtractQuadrantPoints(IReadOnlyList<Response> responses)
    {
        var points = new List<QuadrantPoint>();

        foreach (var response in responses)
        {
            if (TryParseQuadrant(response.Payload, out var point))
            {
                points.Add(point);
            }
        }

        return points;
    }

    private static bool TryParseQuadrant(string payload, out QuadrantPoint point)
    {
        point = new QuadrantPoint(0, 0, null);

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!document.RootElement.TryGetProperty("x", out var xElement)
                || !document.RootElement.TryGetProperty("y", out var yElement))
            {
                return false;
            }

            if (!xElement.TryGetDouble(out var x) || !yElement.TryGetDouble(out var y))
            {
                return false;
            }

            string? label = null;
            if (document.RootElement.TryGetProperty("label", out var labelElement))
            {
                label = labelElement.GetString();
            }
            else if (document.RootElement.TryGetProperty("text", out var textElement))
            {
                label = textElement.GetString();
            }

            point = new QuadrantPoint(x, y, label);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractText(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString();
            }

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (document.RootElement.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString();
            }

            if (document.RootElement.TryGetProperty("word", out var wordElement))
            {
                return wordElement.GetString();
            }

            if (document.RootElement.TryGetProperty("label", out var labelElement))
            {
                return labelElement.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return payload;
        }
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            var cleaned = new string(token
                .Where(ch => char.IsLetterOrDigit(ch) || ch == '-')
                .ToArray());

            if (cleaned.Length == 0)
            {
                continue;
            }

            yield return cleaned.ToLowerInvariant();
        }
    }
}
