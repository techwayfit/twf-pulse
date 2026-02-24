using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized ParticipantRepository.
/// </summary>
public sealed class ParticipantRepository : ParticipantRepositoryBase<PulseMariaDbContext>
{
    public ParticipantRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }
}
