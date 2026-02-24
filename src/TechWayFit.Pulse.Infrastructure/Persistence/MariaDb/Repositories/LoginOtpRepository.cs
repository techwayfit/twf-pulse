using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;

/// <summary>
/// MariaDB-optimized LoginOtpRepository.
/// </summary>
public sealed class LoginOtpRepository : LoginOtpRepositoryBase<PulseMariaDbContext>
{
    public LoginOtpRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }
}
