using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific ParticipantRepository.
/// Inherits all implementation from ParticipantRepositoryBase with server-side sorting.
/// </summary>
public sealed class ParticipantRepository : ParticipantRepositoryBase
{
 public ParticipantRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }

    // ? Uses base implementation with default server-side sorting
    // No overrides needed - base class provides optimal behavior
}
