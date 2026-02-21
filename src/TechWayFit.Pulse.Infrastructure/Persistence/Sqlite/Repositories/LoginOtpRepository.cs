using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific LoginOtpRepository.
/// Inherits all implementation from LoginOtpRepositoryBase with server-side sorting.
/// </summary>
public sealed class LoginOtpRepository : LoginOtpRepositoryBase
{
    public LoginOtpRepository(IPulseDbContext context) : base(context)
    {
    }

  // ? Uses base implementation with default server-side sorting and standard delete
    // No overrides needed - base class provides optimal behavior
}
