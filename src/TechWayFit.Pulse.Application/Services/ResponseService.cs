using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Results;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Commands;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Events;

namespace TechWayFit.Pulse.Application.Services;

public sealed class ResponseService : IResponseService
{
    private readonly IResponseRepository _responses;
    private readonly ISessionRepository _sessions;
    private readonly IActivityRepository _activities;
    private readonly IParticipantRepository _participants;
    private readonly IContributionCounterRepository _counters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public ResponseService(
        IResponseRepository responses,
        ISessionRepository sessions,
        IActivityRepository activities,
        IParticipantRepository participants,
        IContributionCounterRepository counters,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _responses = responses;
        _sessions = sessions;
        _activities = activities;
        _participants = participants;
        _counters = counters;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<Result<Response>> SubmitAsync(
        SubmitResponseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var response = await SubmitAsync(
                command.SessionId,
                command.ActivityId,
                command.ParticipantId,
                command.Payload,
                command.CreatedAt,
                cancellationToken);
            return Result<Response>.Success(response);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Response>.Failure(MapError(ex));
        }
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

        var activity = await _activities.GetByIdAsync(activityId, cancellationToken);
        if (activity is null || activity.SessionId != sessionId)
        {
            throw new InvalidOperationException("Activity not found for this session.");
        }

        var participant = await _participants.GetByIdAsync(participantId, cancellationToken);
        if (participant is null || participant.SessionId != sessionId)
        {
            throw new InvalidOperationException("Participant not found for this session.");
        }

        var domainEvents = Array.Empty<IDomainEvent>();
        var response = await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var counter = await _counters.GetAsync(participantId, sessionId, ct);
                var currentTotal = counter?.TotalContributions ?? 0;

                // Check activity-level limit (e.g., PollConfig.MaxResponsesPerParticipant)
                if (activity.Config is not null)
                {
                    var config = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(activity.Config);
                    if (config.TryGetProperty("MaxResponsesPerParticipant", out var maxResponsesProp)
                        && maxResponsesProp.TryGetInt32(out var maxResponses)
                        && maxResponses > 0)
                    {
                        var activityResponseCount = await _responses.CountByActivityAndParticipantAsync(
                            activityId,
                            participantId,
                            ct);

                        if (activityResponseCount >= maxResponses)
                        {
                            throw new InvalidOperationException($"You have already submitted {activityResponseCount} response(s) to this activity. Maximum allowed is {maxResponses}.");
                        }
                    }
                }

                var submittedResponse = session.SubmitResponse(activity, participant, payload, createdAt);
                await _responses.AddAsync(submittedResponse, ct);

                var updatedCounter = counter ?? new ContributionCounter(
                    participantId,
                    sessionId,
                    currentTotal,
                    createdAt);
                updatedCounter.Increment(createdAt);
                await _counters.UpsertAsync(updatedCounter, ct);

                domainEvents = session.DequeueDomainEvents().ToArray();
                return submittedResponse;
            },
            cancellationToken);

        if (domainEvents.Length > 0)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

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

    private static Error MapError(Exception ex)
    {
        return ex switch
        {
            ArgumentException argumentException => ResultErrors.Validation(argumentException.Message),
            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => new Error("not_found", invalidOperationException.Message, ErrorType.NotFound),
            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("locked", StringComparison.OrdinalIgnoreCase)
                => new Error("forbidden", invalidOperationException.Message, ErrorType.Forbidden),
            InvalidOperationException invalidOperationException => ResultErrors.Validation(invalidOperationException.Message),
            _ => ResultErrors.Unexpected("An unexpected error occurred.")
        };
    }
}
