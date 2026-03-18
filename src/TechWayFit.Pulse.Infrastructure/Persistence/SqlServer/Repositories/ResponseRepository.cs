using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized ResponseRepository with server-side sorting.
/// Inherits all implementation from ResponseRepositoryBase.
/// </summary>
public sealed class ResponseRepository : ResponseRepositoryBase<PulseSqlServerDbContext>
{
    public ResponseRepository(PulseSqlServerDbContext dbContext) : base(dbContext)
    {
    }
}
