using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server-optimized LoginOtpRepository with bulk delete using ExecuteDeleteAsync.
/// </summary>
public sealed class LoginOtpRepository : LoginOtpRepositoryBase<PulseSqlServerDbContext>
{
    public LoginOtpRepository(IDbContextFactory<PulseSqlServerDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    public override async Task DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        await dbContext.LoginOtps
            .Where(o => o.ExpiresAt < before)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
