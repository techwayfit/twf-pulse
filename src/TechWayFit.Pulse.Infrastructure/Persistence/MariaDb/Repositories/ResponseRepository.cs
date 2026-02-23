using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized ResponseRepository.
/// </summary>
public sealed class ResponseRepository : ResponseRepositoryBase
{
    public ResponseRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }
}
