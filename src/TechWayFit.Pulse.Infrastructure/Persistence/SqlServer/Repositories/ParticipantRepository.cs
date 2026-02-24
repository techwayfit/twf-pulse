using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized ParticipantRepository with server-side sorting.
/// Inherits all implementation from ParticipantRepositoryBase.
/// </summary>
public sealed class ParticipantRepository : ParticipantRepositoryBase<PulseSqlServerDbContext>
{
    public ParticipantRepository(IDbContextFactory<PulseSqlServerDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }
}
