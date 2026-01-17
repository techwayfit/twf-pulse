using System.Text.Json;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Services;

public sealed class PollDashboardService : IPollDashboardService
{
    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;
    private readonly IActivityRepository _activities;

    public PollDashboardService(
        IResponseRepository responses,
        IParticipantRepository participants,
        IActivityRepository activities)
    {
        _responses = responses;
        _participants = participants;
        _activities = activities;
    }

    public async Task<PollDashboardResponse> GetPollDashboardAsync(
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

        if (activity.Type != Domain.Enums.ActivityType.Poll)
        {
            throw new ArgumentException("Activity is not a Poll type.", nameof(activityId));
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

        // Parse the poll configuration to get option labels
        var pollConfig = ParsePollConfiguration(activity.Config ?? "{}");
        
        // Count votes for each option
        var optionCounts = CountPollResponses(filteredResponses, pollConfig);
        
        // Calculate percentages
        var totalVotes = optionCounts.Values.Sum();
        var results = optionCounts
            .Select(kvp => new PollOptionResult(
                kvp.Key.Id,
                kvp.Key.Label,
                kvp.Value,
                totalVotes > 0 ? (double)kvp.Value / totalVotes * 100.0 : 0.0))
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Label)
            .ToList();

        return new PollDashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            filteredResponses.Count,
            participants.Count,
            respondedParticipants,
            results,
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

    private static PollConfiguration ParsePollConfiguration(string config)
    {
        try
        {
            using var document = JsonDocument.Parse(config);
            var root = document.RootElement;
            
            var options = new List<PollOption>();
            if (root.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var optionElement in optionsElement.EnumerateArray())
                {
                    var id = optionElement.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "" : "";
                    var label = optionElement.TryGetProperty("label", out var labelElement) ? labelElement.GetString() ?? "" : "";
                    
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(label))
                    {
                        options.Add(new PollOption(id, label));
                    }
                }
            }

            return new PollConfiguration(options);
        }
        catch (JsonException)
        {
            return new PollConfiguration(new List<PollOption>());
        }
    }

    private static Dictionary<PollOption, int> CountPollResponses(
        IReadOnlyList<Domain.Entities.Response> responses,
        PollConfiguration config)
    {
        var counts = config.Options.ToDictionary(option => option, _ => 0);

        foreach (var response in responses)
        {
            var selectedOptions = ParsePollResponse(response.Payload);
            
            foreach (var selectedOptionId in selectedOptions)
            {
                var option = config.Options.FirstOrDefault(o => o.Id == selectedOptionId);
                if (option != null)
                {
                    counts[option]++;
                }
            }
        }

        return counts;
    }

    private static IReadOnlyList<string> ParsePollResponse(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            // Handle array of selected option IDs
            if (root.ValueKind == JsonValueKind.Array)
            {
                var options = new List<string>();
                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var optionId = element.GetString();
                        if (!string.IsNullOrEmpty(optionId))
                        {
                            options.Add(optionId);
                        }
                    }
                }
                return options;
            }

            // Handle object with selectedOptionIds property (current format)
            if (root.TryGetProperty("selectedOptionIds", out var selectedOptionIdsElement) && selectedOptionIdsElement.ValueKind == JsonValueKind.Array)
            {
                var options = new List<string>();
                foreach (var element in selectedOptionIdsElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var optionId = element.GetString();
                        if (!string.IsNullOrEmpty(optionId))
                        {
                            options.Add(optionId);
                        }
                    }
                }
                return options;
            }

            // Handle object with selectedOptions property (legacy format)
            if (root.TryGetProperty("selectedOptions", out var selectedOptionsElement) && selectedOptionsElement.ValueKind == JsonValueKind.Array)
            {
                var options = new List<string>();
                foreach (var element in selectedOptionsElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var optionId = element.GetString();
                        if (!string.IsNullOrEmpty(optionId))
                        {
                            options.Add(optionId);
                        }
                    }
                }
                return options;
            }

            // Handle single string value (single selection)
            if (root.ValueKind == JsonValueKind.String)
            {
                var optionId = root.GetString();
                return !string.IsNullOrEmpty(optionId) ? new[] { optionId } : Array.Empty<string>();
            }

            return Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private sealed record PollConfiguration(IReadOnlyList<PollOption> Options);
    private sealed record PollOption(string Id, string Label);
}