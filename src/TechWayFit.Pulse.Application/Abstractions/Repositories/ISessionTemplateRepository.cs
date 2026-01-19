using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface ISessionTemplateRepository
{
    Task<SessionTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionTemplate>> GetByCategoryAsync(TemplateCategory category, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionTemplate>> GetUserTemplatesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(SessionTemplate template, CancellationToken cancellationToken = default);

    Task UpdateAsync(SessionTemplate template, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
