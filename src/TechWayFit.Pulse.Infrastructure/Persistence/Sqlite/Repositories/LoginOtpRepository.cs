using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.Sqlite;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite-specific LoginOtpRepository.
/// SQLite does not support DateTimeOffset in ORDER BY clauses, so sorted
/// queries materialize results first and then apply client-side ordering.
/// </summary>
public sealed class LoginOtpRepository : LoginOtpRepositoryBase<PulseSqlLiteDbContext>
{
    public LoginOtpRepository(IDbContextFactory<PulseSqlLiteDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    public override async Task<IReadOnlyList<LoginOtp>> GetRecentOtpsForEmailAsync(
        string email,
        int count,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.LoginOtps
            .AsNoTracking()
            .Where(o => o.Email == normalizedEmail)
            .ToListAsync(cancellationToken);

        return records
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .Select(MapToDomain)
            .ToList();
    }
}
