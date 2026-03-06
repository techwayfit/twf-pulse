using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB LoginOtpRepository implementation.
/// </summary>
public sealed class LoginOtpRepository : ILoginOtpRepository
{
    private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

public LoginOtpRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
      _dbContextFactory = dbContextFactory;
    }

    private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<LoginOtp?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.LoginOtps
         .AsNoTracking()
    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<LoginOtp?> GetValidOtpAsync(
        string email,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
  await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        
   var record = await dbContext.LoginOtps
 .AsNoTracking()
   .Where(x => x.Email == email
     && x.OtpCode == otpCode
    && !x.IsUsed
   && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

      return record?.ToDomain();
 }

    public async Task<IReadOnlyList<LoginOtp>> GetRecentOtpsForEmailAsync(
     string email,
        int count,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.LoginOtps
     .AsNoTracking()
            .Where(x => x.Email == email)
            .OrderByDescending(x => x.CreatedAt)
         .Take(count)
   .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
  }

    public async Task AddAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.LoginOtps.Add(otp.ToRecord());
    await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.LoginOtps.FindAsync(new object[] { otp.Id }, cancellationToken);
        if (record is null)
   {
 throw new InvalidOperationException($"LoginOtp with ID {otp.Id} not found.");
  }

      record.Email = otp.Email;
        record.OtpCode = otp.OtpCode;
        record.ExpiresAt = otp.ExpiresAt;
        record.IsUsed = otp.IsUsed;
        record.UsedAt = otp.UsedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpiredAsync(
     DateTimeOffset before,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
     await dbContext.LoginOtps
            .Where(x => x.ExpiresAt < before)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
