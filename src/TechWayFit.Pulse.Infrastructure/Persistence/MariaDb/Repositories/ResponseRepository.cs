using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized ResponseRepository.
/// </summary>
public sealed class ResponseRepository : ResponseRepositoryBase<PulseMariaDbContext>
{
    public ResponseRepository(PulseMariaDbContext dbContext) : base(dbContext)
    {
    }
}
