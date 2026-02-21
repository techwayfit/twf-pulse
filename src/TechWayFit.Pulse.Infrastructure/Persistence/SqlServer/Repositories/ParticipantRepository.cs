using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized ParticipantRepository with server-side sorting.
/// Inherits all implementation from ParticipantRepositoryBase - no SQL Server-specific optimizations needed.
/// </summary>
public sealed class ParticipantRepository : ParticipantRepositoryBase
{
    public ParticipantRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }

    // ? Inherits ALL methods from ParticipantRepositoryBase
    // ? Uses base class ApplySorting (server-side sorting works great in SQL Server)
    // No overrides needed - base class already provides optimal SQL Server behavior
}
