using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for LoginOtp with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class LoginOtpRepositoryBase : ILoginOtpRepository
{
  protected readonly IPulseDbContext _context;

    protected LoginOtpRepositoryBase(IPulseDbContext context)
    {
        _context = context;
    }

    // ? Shared implementation - no duplication
    public async Task<LoginOtp?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
        var record = await _context.LoginOtps
   .AsNoTracking()
  .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    // ? Shared implementation - no duplication (already optimized)
    public async Task<LoginOtp?> GetValidOtpAsync(
        string email,
        string otpCode,
   CancellationToken cancellationToken = default)
    {
  var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedOtp = otpCode.Trim();
  var now = DateTimeOffset.UtcNow;

        // Server-side filtering for all providers
      var record = await _context.LoginOtps
 .AsNoTracking()
            .Where(o => o.Email == normalizedEmail
             && o.OtpCode == normalizedOtp
      && !o.IsUsed
   && o.ExpiresAt > now)
      .FirstOrDefaultAsync(cancellationToken);

  return record == null ? null : MapToDomain(record);
    }

    // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyList<LoginOtp>> GetRecentOtpsForEmailAsync(
     string email,
  int count,
        CancellationToken cancellationToken = default)
    {
  var normalizedEmail = email.Trim().ToLowerInvariant();

  var query = _context.LoginOtps
  .AsNoTracking()
     .Where(o => o.Email == normalizedEmail);

     // Apply provider-specific sorting
        query = ApplySorting(query);

   var records = await query
     .Take(count)
  .ToListAsync(cancellationToken);

      return records.Select(MapToDomain).ToList();
    }

    // ? Shared implementation - no duplication
    public async Task AddAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(otp);
        await _context.LoginOtps.AddAsync(record, cancellationToken);
  await _context.SaveChangesAsync(cancellationToken);
    }

    // ? Shared implementation - no duplication
    public async Task UpdateAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
var record = MapToRecord(otp);
     _context.LoginOtps.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ? Virtual method - can be overridden for provider-specific bulk delete optimization
    public virtual async Task DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
     // Default: Load into memory then delete (works for all providers)
   var expiredOtps = await _context.LoginOtps
      .Where(o => o.ExpiresAt < before)
  .ToListAsync(cancellationToken);

        _context.LoginOtps.RemoveRange(expiredOtps);
  await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Virtual method for provider-specific sorting implementation.
    /// Override in derived classes for optimal performance.
    /// </summary>
    protected virtual IQueryable<LoginOtpRecord> ApplySorting(IQueryable<LoginOtpRecord> query)
{
        // Default: Server-side sorting (works for most providers)
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
