using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Events;

namespace TechWayFit.Pulse.Web.Services;

public sealed class LoggingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<LoggingDomainEventDispatcher> _logger;

    public LoggingDomainEventDispatcher(ILogger<LoggingDomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            switch (domainEvent)
            {
                case ResponseSubmittedDomainEvent submitted:
                    _logger.LogInformation(
                        "Domain event: ResponseSubmitted SessionId={SessionId} ActivityId={ActivityId} ParticipantId={ParticipantId} ResponseId={ResponseId}",
                        submitted.SessionId,
                        submitted.ActivityId,
                        submitted.ParticipantId,
                        submitted.ResponseId);
                    break;
                default:
                    _logger.LogInformation("Domain event: {EventType}", domainEvent.GetType().Name);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
