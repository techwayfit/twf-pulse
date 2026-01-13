using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface IFacilitatorUserRepository
{
    Task<FacilitatorUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FacilitatorUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FacilitatorUser>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(FacilitatorUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(FacilitatorUser user, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default);
}
