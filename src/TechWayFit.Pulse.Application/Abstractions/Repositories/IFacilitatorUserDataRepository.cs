using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface IFacilitatorUserDataRepository
{
    Task<FacilitatorUserData?> GetByKeyAsync(Guid facilitatorUserId, string key, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<FacilitatorUserData>> GetAllByUserIdAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, string>> GetAllAsDictAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default);
    
    Task AddAsync(FacilitatorUserData data, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(FacilitatorUserData data, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(Guid facilitatorUserId, string key, CancellationToken cancellationToken = default);
    
    Task SetValueAsync(Guid facilitatorUserId, string key, string value, CancellationToken cancellationToken = default);
}
