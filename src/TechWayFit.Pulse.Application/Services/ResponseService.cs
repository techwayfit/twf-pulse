using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Services;

public sealed class ResponseService : IResponseService
{
    private readonly IResponseRepository _responses;
    private readonly ISessionRepository _sessions;
    private readonly IActivityRepository _activities;
    private readonly IParticipantRepository _participants;
    private readonly IContributionCounterRepository _counters;

    public ResponseService(
        IResponseRepository responses,
        ISessionRepository sessions,
        IActivityRepository activities,
        IParticipantRepository participants,
        IContributionCounterRepository counters)
    {
        _responses = responses;
        _sessions = sessions;
        _activities = activities;
        _participants = participants;
        _counters = counters;
    }

    public async Task<Response> SubmitAsync(
        Guid sessionId,
        Guid activityId,
        Guid participantId,
        string payload,
        DateTimeOffset createdAt,
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

        if (participantId == Guid.Empty)
        {
            throw new ArgumentException("Participant id is required.", nameof(participantId));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Response payload is required.", nameof(payload));
        }

        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        if (session.Status != Domain.Enums.SessionStatus.Live)
        {
            throw new InvalidOperationException("Session is not live.");
        }

        if (session.Settings.StrictCurrentActivityOnly && session.CurrentActivityId != activityId)
        {
            throw new InvalidOperationException("Responses are locked to the current activity.");
        }

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity is null || activity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity not found for this session.");
        }

        if (activity.Status != Domain.Enums.ActivityStatus.Open)
        {
            throw new InvalidOperationException("Activity is not open.");
        }

        var participant = await _participants.GetByIdAsync(participantId, cancellationToken);
        if (participant is null || participant.SessionId != sessionId)
        {
            throw new InvalidOperationException("Participant not found for this session.");
        }

        var counter = await _counters.GetAsync(participantId, sessionId, cancellationToken);
        var currentTotal = counter?.TotalContributions ?? 0;
         
        // Check activity-level limit (e.g., PollConfig.MaxResponsesPerParticipant)
        if (activity.Config is not null)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(activity.Config);
            if (config.TryGetProperty("MaxResponsesPerParticipant", out var maxResponsesProp) && 
                maxResponsesProp.TryGetInt32(out var maxResponses) && 
                maxResponses > 0)
            {
                var activityResponseCount = await _responses.CountByActivityAndParticipantAsync(
                    activityId,
                    participantId,
                    cancellationToken);

                if (activityResponseCount >= maxResponses)
                {
                    throw new InvalidOperationException($"You have already submitted {activityResponseCount} response(s) to this activity. Maximum allowed is {maxResponses}.");
                }
            }
        }

        var response = new Response(
            Guid.NewGuid(),
            sessionId,
            activityId,
            participantId,
            payload.Trim(),
            participant.Dimensions,
            createdAt);

        await _responses.AddAsync(response, cancellationToken);
        var updatedCounter = counter ?? new Domain.Entities.ContributionCounter(
            participantId,
            sessionId,
            currentTotal,
            createdAt);
        updatedCounter.Increment(createdAt);
        await _counters.UpsertAsync(updatedCounter, cancellationToken);
        return response;
    }

    public Task<IReadOnlyList<Response>> GetByActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        return _responses.GetByActivityAsync(activityId, cancellationToken);
    }

    public Task<IReadOnlyList<Response>> GetByParticipantAsync(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        return _responses.GetByParticipantAsync(sessionId, participantId, cancellationToken);
    }
}
