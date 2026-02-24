using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for LoginOtp with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class LoginOtpRepositoryBase<TContext> : ILoginOtpRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    protected LoginOtpRepositoryBase(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<LoginOtp?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.LoginOtps
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<LoginOtp?> GetValidOtpAsync(
        string email,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedOtp = otpCode.Trim();
        var now = DateTimeOffset.UtcNow;

        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.LoginOtps
            .AsNoTracking()
            .Where(o => o.Email == normalizedEmail
                && o.OtpCode == normalizedOtp
                && !o.IsUsed
                && o.ExpiresAt > now)
            .FirstOrDefaultAsync(cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public virtual async Task<IReadOnlyList<LoginOtp>> GetRecentOtpsForEmailAsync(
        string email,
        int count,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.LoginOtps
            .AsNoTracking()
            .Where(o => o.Email == normalizedEmail);

        query = ApplySorting(query);

        var records = await query
            .Take(count)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task AddAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(otp);
        await dbContext.LoginOtps.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(otp);
        dbContext.LoginOtps.Update(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var expiredOtps = await dbContext.LoginOtps
            .Where(o => o.ExpiresAt < before)
            .ToListAsync(cancellationToken);

        dbContext.LoginOtps.RemoveRange(expiredOtps);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    protected virtual IQueryable<LoginOtpRecord> ApplySorting(IQueryable<LoginOtpRecord> query)
    {
        return query.OrderByDescending(o => o.CreatedAt);
    }

    protected static LoginOtp MapToDomain(LoginOtpRecord record)
    {
        return new LoginOtp(
            record.Id,
            record.Email,
            record.OtpCode,
            record.CreatedAt,
            record.ExpiresAt,
            record.IsUsed,
            record.UsedAt);
    }

    protected static LoginOtpRecord MapToRecord(LoginOtp otp)
    {
        return new LoginOtpRecord
        {
            Id = otp.Id,
            Email = otp.Email,
            OtpCode = otp.OtpCode,
            CreatedAt = otp.CreatedAt,
            ExpiresAt = otp.ExpiresAt,
            IsUsed = otp.IsUsed,
            UsedAt = otp.UsedAt
        };
    }
}
