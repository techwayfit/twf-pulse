using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized ParticipantRepository.
/// </summary>
public sealed class ParticipantRepository : ParticipantRepositoryBase
{
    public ParticipantRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }
}
