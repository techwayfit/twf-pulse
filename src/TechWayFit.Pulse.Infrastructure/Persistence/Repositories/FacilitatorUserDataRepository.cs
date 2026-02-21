using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared FacilitatorUserDataRepository implementation.
/// </summary>
public class FacilitatorUserDataRepository : IFacilitatorUserDataRepository
{
    protected readonly IPulseDbContext _context;

    public FacilitatorUserDataRepository(IPulseDbContext context)
    {
        _context = context;
    }

    public async Task<FacilitatorUserData?> GetByKeyAsync(Guid facilitatorUserId, string key, CancellationToken cancellationToken = default)
    {
        var record = await _context.FacilitatorUserData
            .FirstOrDefaultAsync(d => d.FacilitatorUserId == facilitatorUserId && d.Key == key, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<IReadOnlyList<FacilitatorUserData>> GetAllByUserIdAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        var records = await _context.FacilitatorUserData
            .Where(d => d.FacilitatorUserId == facilitatorUserId)
            .OrderBy(d => d.Key)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<Dictionary<string, string>> GetAllAsDictAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        var records = await _context.FacilitatorUserData
            .Where(d => d.FacilitatorUserId == facilitatorUserId)
            .ToListAsync(cancellationToken);

        return records.ToDictionary(r => r.Key, r => r.Value);
    }

    public async Task AddAsync(FacilitatorUserData data, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(data);
        await _context.FacilitatorUserData.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FacilitatorUserData data, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(data);
        _context.FacilitatorUserData.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid facilitatorUserId, string key, CancellationToken cancellationToken = default)
    {
        var record = await _context.FacilitatorUserData
            .FirstOrDefaultAsync(d => d.FacilitatorUserId == facilitatorUserId && d.Key == key, cancellationToken);

        if (record != null)
        {
            _context.FacilitatorUserData.Remove(record);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetValueAsync(Guid facilitatorUserId, string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await _context.FacilitatorUserData
            .FirstOrDefaultAsync(d => d.FacilitatorUserId == facilitatorUserId && d.Key == key, cancellationToken);

        if (existing != null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            _context.FacilitatorUserData.Update(existing);
        }
        else
        {
            var newRecord = new FacilitatorUserDataRecord
            {
                Id = Guid.NewGuid(),
                FacilitatorUserId = facilitatorUserId,
                Key = key,
                Value = value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _context.FacilitatorUserData.AddAsync(newRecord, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    protected static FacilitatorUserData MapToDomain(FacilitatorUserDataRecord record)
    {
        return new FacilitatorUserData(
            record.Id,
            record.FacilitatorUserId,
            record.Key,
            record.Value,
            record.CreatedAt,
            record.UpdatedAt);
    }

    protected static FacilitatorUserDataRecord MapToRecord(FacilitatorUserData data)
    {
        return new FacilitatorUserDataRecord
        {
            Id = data.Id,
            FacilitatorUserId = data.FacilitatorUserId,
            Key = data.Key,
            Value = data.Value,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt
        };
    }
}
