using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized ResponseRepository with server-side sorting.
/// Inherits all implementation from ResponseRepositoryBase - no SQL Server-specific optimizations needed.
/// </summary>
public sealed class ResponseRepository : ResponseRepositoryBase
{
  public ResponseRepository(IPulseDbContext dbContext) : base(dbContext)
  {
    }

    // ? Inherits ALL methods from ResponseRepositoryBase
    // ? Uses base class ApplySorting (server-side sorting works great in SQL Server)
    // No overrides needed - base class already provides optimal SQL Server behavior
}
