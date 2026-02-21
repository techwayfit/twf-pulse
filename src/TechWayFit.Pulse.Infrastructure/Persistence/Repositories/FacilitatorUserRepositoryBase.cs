using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for FacilitatorUser with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class FacilitatorUserRepositoryBase : IFacilitatorUserRepository
{
    protected readonly IPulseDbContext _context;

    protected FacilitatorUserRepositoryBase(IPulseDbContext context)
{
    _context = context;
    }

    // ? Shared implementation - no duplication
 public async Task<FacilitatorUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
     var record = await _context.FacilitatorUsers
  .AsNoTracking()
      .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    // ? Shared implementation - no duplication
    public async Task<FacilitatorUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
  var normalizedEmail = email.Trim().ToLowerInvariant();

        var record = await _context.FacilitatorUsers
.AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

 return record == null ? null : MapToDomain(record);
    }

 // ? Template method - uses virtual ApplySorting
    public virtual async Task<IReadOnlyList<FacilitatorUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
    var query = _context.FacilitatorUsers
        .AsNoTracking();

// Apply provider-specific sorting
      query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(MapToDomain).ToList();
    }

    // ? Shared implementation - no duplication
    public async Task AddAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(user);
await _context.FacilitatorUsers.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ? Shared implementation - no duplication
public async Task UpdateAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
    {
   var record = MapToRecord(user);
   _context.FacilitatorUsers.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ? Shared implementation - no duplication
    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
      var normalizedEmail = email.Trim().ToLowerInvariant();
  return await _context.FacilitatorUsers
     .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Virtual method for provider-specific sorting implementation.
    /// Override in derived classes for optimal performance.
    /// </summary>
    protected virtual IQueryable<FacilitatorUserRecord> ApplySorting(IQueryable<FacilitatorUserRecord> query)
    {
   // Default: Server-side sorting (works for most providers)
     return query.OrderBy(u => u.CreatedAt);
    }

    protected static FacilitatorUser MapToDomain(FacilitatorUserRecord record)
    {
        return new FacilitatorUser(
      record.Id,
    record.Email,
     record.DisplayName,
      record.CreatedAt,
 record.LastLoginAt);
    }

    protected static FacilitatorUserRecord MapToRecord(FacilitatorUser user)
    {
return new FacilitatorUserRecord
 {
    Id = user.Id,
    Email = user.Email,
     DisplayName = user.DisplayName,
   CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
   };
    }
}
