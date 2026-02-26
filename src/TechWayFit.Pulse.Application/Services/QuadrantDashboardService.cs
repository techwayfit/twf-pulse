using System.Text.Json;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;

namespace TechWayFit.Pulse.Application.Services;

public sealed class QuadrantDashboardService : IQuadrantDashboardService
{
    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;
    private readonly IActivityRepository _activities;

    public QuadrantDashboardService(
        IResponseRepository responses,
        IParticipantRepository participants,
        IActivityRepository activities)
    {
        _responses = responses;
        _participants = participants;
        _activities = activities;
    }

    public async Task<QuadrantDashboardResponse> GetQuadrantDashboardAsync(
        Guid sessionId,
        Guid activityId,
        IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session id is required.", nameof(sessionId));

        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity id is required.", nameof(activityId));

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken)
            ?? throw new ArgumentException("Activity not found.", nameof(activityId));

        if (activity.Type != ActivityType.Quadrant)
            throw new ArgumentException("Activity is not of Quadrant type.", nameof(activityId));

        // Parse config for item labels and axis names
        var config = ParseConfig(activity.Config);

        var responses = await _responses.GetByActivityAsync(activityId, cancellationToken);
        var filtered = ApplyFilters(responses, filters);

        var participants = await _participants.GetBySessionAsync(sessionId, cancellationToken);
        var respondedParticipants = filtered
            .Select(r => r.ParticipantId)
            .Distinct()
            .Count();

        var lastResponseAt = filtered.Count == 0
            ? (DateTimeOffset?)null
            : filtered.Max(r => r.CreatedAt);

        var items = BuildItemAggregates(filtered, config);

        return new QuadrantDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            config?.XAxisLabel ?? "X",
            config?.YAxisLabel ?? "Y",
            filtered.Count,
            participants.Count,
            respondedParticipants,
            items,
            lastResponseAt);
    }

    private static QuadrantConfig? ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<QuadrantConfig>(
                configJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<QuadrantItemAggregate> BuildItemAggregates(
        IReadOnlyList<Domain.Entities.Response> responses,
        QuadrantConfig? config)
    {
        var groups = new Dictionary<int, List<(double x, double y, string? note, DateTimeOffset createdAt)>>();

        foreach (var response in responses)
        {
            if (!TryParseItemResponse(response.Payload, out var itemIndex, out var x, out var y, out var note))
                continue;

            if (!groups.TryGetValue(itemIndex, out var list))
            {
                list = new List<(double, double, string?, DateTimeOffset)>();
                groups[itemIndex] = list;
            }

            list.Add((x, y, note, response.CreatedAt));
        }

        var aggregates = new List<QuadrantItemAggregate>();
        var itemLabels = config?.Items ?? new List<string>();

        foreach (var (idx, scores) in groups.OrderBy(g => g.Key))
        {
            var label = idx >= 0 && idx < itemLabels.Count
                ? itemLabels[idx]
                : $"Item {idx + 1}";

            var avgX = scores.Average(s => s.x);
            var avgY = scores.Average(s => s.y);

            var notes = scores
                .Where(s => !string.IsNullOrWhiteSpace(s.note))
                .Select(s => new QuadrantItemNote(s.note!, s.createdAt))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            aggregates.Add(new QuadrantItemAggregate(idx, label, avgX, avgY, scores.Count, notes));
        }

        return aggregates;
    }

    private static bool TryParseItemResponse(
        string payload,
        out int itemIndex,
        out double x,
        out double y,
        out string? note)
    {
        itemIndex = 0;
        x = 0;
        y = 0;
        note = null;

        if (string.IsNullOrWhiteSpace(payload)) return false;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return false;

            if (!root.TryGetProperty("itemIndex", out var idxEl) || !idxEl.TryGetInt32(out itemIndex))
                return false;

            // xValue / yValue are stored as strings (the raw ScoreOption.Value)
            if (!root.TryGetProperty("xValue", out var xEl)) return false;
            if (!root.TryGetProperty("yValue", out var yEl)) return false;

            var xStr = xEl.ValueKind == JsonValueKind.String ? xEl.GetString() : xEl.GetRawText();
            var yStr = yEl.ValueKind == JsonValueKind.String ? yEl.GetString() : yEl.GetRawText();

            if (!double.TryParse(xStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out x)) return false;
            if (!double.TryParse(yStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out y)) return false;

            if (root.TryGetProperty("note", out var noteEl) &&
                noteEl.ValueKind == JsonValueKind.String)
            {
                note = noteEl.GetString();
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static List<Domain.Entities.Response> ApplyFilters(
        IReadOnlyList<Domain.Entities.Response> responses,
        IReadOnlyDictionary<string, string?> filters)
    {
        if (filters.Count == 0) return responses.ToList();

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
            if (string.IsNullOrWhiteSpace(filter.Value)) continue;
            if (!dimensions.TryGetValue(filter.Key, out var value)) return false;
            if (!string.Equals(value, filter.Value, StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }
}
