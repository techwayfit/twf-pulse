using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

public sealed class LoginOtpRepository : ILoginOtpRepository
{
    private readonly PulseDbContext _context;

    public LoginOtpRepository(PulseDbContext context)
    {
        _context = context;
    }

    public async Task<LoginOtp?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _context.LoginOtps
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

        var record = await _context.LoginOtps
   .AsNoTracking()
     .Where(o => o.Email == normalizedEmail 
                && o.OtpCode == normalizedOtp 
    && !o.IsUsed)
         .ToListAsync(cancellationToken);

        var validRecord = record.FirstOrDefault(o => o.ExpiresAt > now);

        return validRecord == null ? null : MapToDomain(validRecord);
    }

    public async Task<IReadOnlyList<LoginOtp>> GetRecentOtpsForEmailAsync(
   string email,
        int count,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var records = await _context.LoginOtps
 .AsNoTracking()
     .Where(o => o.Email == normalizedEmail)
  .Take(count)
         .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        return records
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapToDomain)
            .ToList();
    }

    public async Task AddAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(otp);
        await _context.LoginOtps.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(otp);
        _context.LoginOtps.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
        var expiredOtps = await _context.LoginOtps
        .Where(o => o.ExpiresAt < before)
         .ToListAsync(cancellationToken);

        _context.LoginOtps.RemoveRange(expiredOtps);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static LoginOtp MapToDomain(LoginOtpRecord record)
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

    private static LoginOtpRecord MapToRecord(LoginOtp otp)
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
