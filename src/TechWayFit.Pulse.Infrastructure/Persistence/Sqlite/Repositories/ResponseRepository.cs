using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific ResponseRepository.
/// Inherits all implementation from ResponseRepositoryBase with server-side sorting.
/// </summary>
public sealed class ResponseRepository : ResponseRepositoryBase
{
    public ResponseRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }

    // ? Uses base implementation with default server-side sorting
    // No overrides needed - base class provides optimal behavior
}
