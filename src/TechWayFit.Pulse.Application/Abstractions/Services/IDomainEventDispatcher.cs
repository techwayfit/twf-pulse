using TechWayFit.Pulse.Domain.Events;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
