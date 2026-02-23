using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized LoginOtpRepository.
/// </summary>
public sealed class LoginOtpRepository : LoginOtpRepositoryBase
{
    public LoginOtpRepository(IPulseDbContext dbContext) : base(dbContext)
    {
    }
}
