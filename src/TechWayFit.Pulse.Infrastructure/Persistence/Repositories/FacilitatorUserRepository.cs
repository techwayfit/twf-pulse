using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

public sealed class FacilitatorUserRepository : IFacilitatorUserRepository
{
    private readonly PulseDbContext _context;

    public FacilitatorUserRepository(PulseDbContext context)
    {
        _context = context;
    }

    public async Task<FacilitatorUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
    var record = await _context.FacilitatorUsers
 .AsNoTracking()
 .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<FacilitatorUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
   var normalizedEmail = email.Trim().ToLowerInvariant();

    var record = await _context.FacilitatorUsers
            .AsNoTracking()
   .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<IReadOnlyList<FacilitatorUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await _context.FacilitatorUsers
 .AsNoTracking()
 .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records.OrderBy(u => u.CreatedAt).ToList();

        return sortedRecords.Select(MapToDomain).ToList();
    }

    public async Task AddAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(user);
        await _context.FacilitatorUsers.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
    {
     var record = MapToRecord(user);
        _context.FacilitatorUsers.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

  public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
      var normalizedEmail = email.Trim().ToLowerInvariant();
return await _context.FacilitatorUsers
       .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    private static FacilitatorUser MapToDomain(FacilitatorUserRecord record)
    {
        return new FacilitatorUser(
            record.Id,
            record.Email,
            record.DisplayName,
            record.CreatedAt,
            record.LastLoginAt);
    }

    private static FacilitatorUserRecord MapToRecord(FacilitatorUser user)
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
