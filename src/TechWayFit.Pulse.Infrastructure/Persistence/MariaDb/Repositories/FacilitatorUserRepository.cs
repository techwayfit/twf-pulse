using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized FacilitatorUserRepository.
/// </summary>
public sealed class FacilitatorUserRepository : FacilitatorUserRepositoryBase<PulseMariaDbContext>
{
    public FacilitatorUserRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }
}
