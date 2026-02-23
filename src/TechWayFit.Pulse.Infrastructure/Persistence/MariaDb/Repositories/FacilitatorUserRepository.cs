using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized FacilitatorUserRepository.
/// </summary>
public sealed class FacilitatorUserRepository : FacilitatorUserRepositoryBase
{
    public FacilitatorUserRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }
}
