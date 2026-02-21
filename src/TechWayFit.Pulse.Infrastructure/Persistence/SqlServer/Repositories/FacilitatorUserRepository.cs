using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized FacilitatorUserRepository with server-side sorting.
/// Inherits all implementation from FacilitatorUserRepositoryBase - no SQL Server-specific optimizations needed.
/// </summary>
public sealed class FacilitatorUserRepository : FacilitatorUserRepositoryBase
{
    public FacilitatorUserRepository(IPulseDbContext context) : base(context)
    {
    }

    // ? Inherits ALL methods from FacilitatorUserRepositoryBase
    // ? Uses base class ApplySorting (server-side sorting works great in SQL Server)
    // No overrides needed - base class already provides optimal SQL Server behavior
}
