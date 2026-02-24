using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized FacilitatorUserRepository with server-side sorting.
/// </summary>
public sealed class FacilitatorUserRepository : FacilitatorUserRepositoryBase<PulseSqlServerDbContext>
{
    public FacilitatorUserRepository(IDbContextFactory<PulseSqlServerDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }
}
