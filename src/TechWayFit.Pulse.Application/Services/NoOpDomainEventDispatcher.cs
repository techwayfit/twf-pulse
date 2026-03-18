using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Events;

namespace TechWayFit.Pulse.Application.Services;

public sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        _ = domainEvents;
        return Task.CompletedTask;
    }
}
