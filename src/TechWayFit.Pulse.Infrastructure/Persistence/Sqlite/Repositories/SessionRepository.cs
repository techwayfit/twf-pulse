using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific SessionRepository.
/// Inherits all implementation from SessionRepositoryBase with server-side sorting.
/// </summary>
public sealed class SessionRepository : SessionRepositoryBase
{
  public SessionRepository(IPulseDbContext dbContext) : base(dbContext)
{
  }

    // ? Uses base implementation with default server-side sorting
    // SQLite handles OrderBy on DateTimeOffset columns correctly
    // No overrides needed - base class provides optimal behavior
}
