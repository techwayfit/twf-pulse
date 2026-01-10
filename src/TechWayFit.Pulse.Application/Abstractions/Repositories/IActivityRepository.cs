using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Activity>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default);
}
