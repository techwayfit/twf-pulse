using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized LoginOtpRepository with bulk delete using ExecuteDeleteAsync.
/// Overrides DeleteExpiredAsync for SQL Server-specific bulk operation optimization.
/// </summary>
public sealed class LoginOtpRepository : LoginOtpRepositoryBase
{
    public LoginOtpRepository(IPulseDbContext context) : base(context)
    {
    }

    // ? Override: SQL Server bulk delete optimization using ExecuteDeleteAsync (EF Core 7+)
  public override async Task DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
// SQL Server optimization: Single DELETE statement instead of loading into memory
        await _context.LoginOtps
            .Where(o => o.ExpiresAt < before)
   .ExecuteDeleteAsync(cancellationToken);
    }

    // ? Inherits all other methods from LoginOtpRepositoryBase (GetByIdAsync, GetValidOtpAsync, GetRecentOtpsForEmailAsync, AddAsync, UpdateAsync)
 // ? Uses base class ApplySorting (server-side sorting works great in SQL Server)
}
