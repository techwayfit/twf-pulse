using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific FacilitatorUserRepository.
/// Inherits all implementation from FacilitatorUserRepositoryBase with server-side sorting.
/// </summary>
public sealed class FacilitatorUserRepository : FacilitatorUserRepositoryBase
{
  public FacilitatorUserRepository(IPulseDbContext context) : base(context)
    {
    }

    // ? Uses base implementation with default server-side sorting
    // No overrides needed - base class provides optimal behavior
}
