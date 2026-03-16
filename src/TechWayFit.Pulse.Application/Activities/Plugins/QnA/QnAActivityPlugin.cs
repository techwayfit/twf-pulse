using System.Text.Json;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Domain.Models.ResponsePayloads;

namespace TechWayFit.Pulse.Application.Activities.Plugins.QnA;

/// <summary>
/// Self-contained plugin for the <see cref="ActivityType.QnA"/> activity type.
/// </summary>
public sealed class QnAActivityPlugin : IActivityPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public ActivityType ActivityType => ActivityType.QnA;

    public ActivityPluginMetadata Metadata { get; } = new(
        DisplayName: "Q&A",
        FaIconClass: "fa-solid fa-comments",
        BadgeColorHex: "#0dcaf0",
        BadgeTextColorHex: "#000000",
        AcceptsResponses: true,
        ConfigType: typeof(QnAConfig),
        ResponsePayloadType: typeof(QnAResponsePayload));

    // ── Config contract ───────────────────────────────────────────────────────

    public string GetDefaultConfig() =>
        """{"allowAnonymous":true,"maxQuestionsPerParticipant":3,"allowUpvoting":true,"maxQuestionLength":300,"requireModeration":false}""";

    public string EnforceConfigLimits(string? config, IActivityDefaults defaults)
    {
        if (string.IsNullOrWhiteSpace(config))
            return GetDefaultConfig();

        try
        {
            // Validate the config is parseable; return as-is since no server limits apply.
            using var doc = JsonDocument.Parse(config);
            return config;
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
            list.Add("Q&A config is required.");
            errors = list;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            var root = doc.RootElement;

            if (root.TryGetProperty("maxQuestionsPerParticipant", out var maxElem)
                && maxElem.TryGetInt32(out var max)
                && max < 1)
            {
                list.Add("maxQuestionsPerParticipant must be at least 1.");
            }
        }
        catch (JsonException ex)
        {
            list.Add($"Q&A config is not valid JSON: {ex.Message}");
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

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("type", out _))
            {
                error = null;
                return true;
            }

            error = "Q&A response payload must be an object with a 'type' property (\"question\" or \"vote\").";
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
        var participants = await dataContext.GetParticipantsAsync(sessionId, cancellationToken);

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
            catch (JsonException) { }
        }

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

        var respondedParticipants = allResponses.Select(r => r.ParticipantId).Distinct().Count();
        var lastResponseAt        = allResponses.Count == 0 ? (DateTimeOffset?)null : allResponses.Max(r => r.CreatedAt);

        var result = new QnADashboardResponse(
            sessionId,
            activityId,
            activity.Title,
            questions.Count,
            votesByQuestionId.Values.Sum(),
            participants.Count,
            respondedParticipants,
            questions,
            lastResponseAt);

        return new QnADashboardData(result);
    }
}
