using System.Text.Json;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Services;

public sealed class QnADashboardService : IQnADashboardService
{
    private readonly IResponseRepository _responses;
    private readonly IParticipantRepository _participants;
    private readonly IActivityRepository _activities;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public QnADashboardService(
        IResponseRepository responses,
        IParticipantRepository participants,
        IActivityRepository activities)
    {
        _responses = responses;
        _participants = participants;
        _activities = activities;
    }

    public async Task<QnADashboardResponse> GetQnADashboardAsync(
        Guid sessionId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session id is required.", nameof(sessionId));

        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity id is required.", nameof(activityId));

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity == null)
            throw new ArgumentException("Activity not found.", nameof(activityId));

        var allResponses = await _responses.GetByActivityAsync(activityId, cancellationToken);
        var participants = await _participants.GetBySessionAsync(sessionId, cancellationToken);

        // Split into question responses and vote responses
        var questionResponses = new List<(Domain.Entities.Response Response, string Text, bool IsAnonymous, bool IsAnswered)>();
        var votesByQuestionId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var response in allResponses)
        {
            if (string.IsNullOrWhiteSpace(response.Payload))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(response.Payload);
                var root = doc.RootElement;

                var type = root.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString() ?? ""
                    : "";

                if (string.Equals(type, "question", StringComparison.OrdinalIgnoreCase))
                {
                    var text = root.TryGetProperty("text", out var textProp)
                        ? textProp.GetString() ?? string.Empty
                        : string.Empty;

                    var isAnonymous = root.TryGetProperty("isAnonymous", out var anonProp)
                        && anonProp.GetBoolean();

                    var isAnswered = root.TryGetProperty("isAnswered", out var answeredProp)
                        && answeredProp.GetBoolean();

                    if (!string.IsNullOrWhiteSpace(text))
                        questionResponses.Add((response, text, isAnonymous, isAnswered));
                }
                else if (string.Equals(type, "vote", StringComparison.OrdinalIgnoreCase))
                {
                    var questionResponseId = root.TryGetProperty("questionResponseId", out var qidProp)
                        ? qidProp.GetString() ?? string.Empty
                        : string.Empty;

                    if (!string.IsNullOrWhiteSpace(questionResponseId))
                    {
                        votesByQuestionId.TryGetValue(questionResponseId, out var existing);
                        votesByQuestionId[questionResponseId] = existing + 1;
                    }
                }
            }
            catch (JsonException)
            {
                // Skip malformed payloads
            }
        }

        // Build QnAQuestionItems sorted by upvote count desc, then by submission time asc
        var questions = questionResponses
            .Select(r =>
            {
                var idStr = r.Response.Id.ToString();
                votesByQuestionId.TryGetValue(idStr, out var votes);
                return new QnAQuestionItem(
                    r.Response.Id,
                    r.Text,
                    votes,
                    r.IsAnonymous,
                    r.IsAnswered,
                    r.Response.CreatedAt,
                    r.Response.ParticipantId);
            })
            .OrderByDescending(q => q.UpvoteCount)
            .ThenBy(q => q.SubmittedAt)
            .ToList();

        var respondedParticipants = allResponses
            .Select(r => r.ParticipantId)
            .Distinct()
            .Count();

        var lastResponseAt = allResponses.Count == 0
            ? (DateTimeOffset?)null
            : allResponses.Max(r => r.CreatedAt);

        return new QnADashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            questions.Count,
            votesByQuestionId.Values.Sum(),
            participants.Count,
            respondedParticipants,
            questions,
            lastResponseAt);
    }

    public async Task ToggleAnsweredAsync(
        Guid questionResponseId,
        bool isAnswered,
        CancellationToken cancellationToken = default)
    {
        var response = await _responses.GetByIdAsync(questionResponseId, cancellationToken)
            ?? throw new ArgumentException("Response not found.", nameof(questionResponseId));

        // Merge isAnswered flag into the existing JSON payload
        using var doc = JsonDocument.Parse(response.Payload);
        var dict = doc.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => (object?)p.Value.Clone());
        dict["isAnswered"] = isAnswered;

        var updatedPayload = JsonSerializer.Serialize(dict);
        await _responses.UpdatePayloadAsync(questionResponseId, updatedPayload, cancellationToken);
    }
}
