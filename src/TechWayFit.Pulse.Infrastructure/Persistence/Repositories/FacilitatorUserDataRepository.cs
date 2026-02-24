using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared FacilitatorUserDataRepository implementation for all providers.
/// </summary>
public class FacilitatorUserDataRepository<TContext> : IFacilitatorUserDataRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    public FacilitatorUserDataRepository(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<FacilitatorUserData?> GetByKeyAsync(Guid facilitatorUserId, string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.FacilitatorUserData
            .FirstOrDefaultAsync(d => d.FacilitatorUserId == facilitatorUserId && d.Key == key, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<IReadOnlyList<FacilitatorUserData>> GetAllByUserIdAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.FacilitatorUserData
            .Where(d => d.FacilitatorUserId == facilitatorUserId)
            .OrderBy(d => d.Key)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<Dictionary<string, string>> GetAllAsDictAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.FacilitatorUserData
            .Where(d => d.FacilitatorUserId == facilitatorUserId)
            .ToListAsync(cancellationToken);

        return records.ToDictionary(r => r.Key, r => r.Value);
    }

    public async Task AddAsync(FacilitatorUserData data, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(data);
        await dbContext.FacilitatorUserData.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FacilitatorUserData data, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(data);
        dbContext.FacilitatorUserData.Update(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid facilitatorUserId, string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.FacilitatorUserData
            .FirstOrDefaultAsync(d => d.FacilitatorUserId == facilitatorUserId && d.Key == key, cancellationToken);

        if (record != null)
        {
            dbContext.FacilitatorUserData.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetValueAsync(Guid facilitatorUserId, string key, string value, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.FacilitatorUserData
            .FirstOrDefaultAsync(d => d.FacilitatorUserId == facilitatorUserId && d.Key == key, cancellationToken);

        if (existing != null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.FacilitatorUserData.Update(existing);
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
            await dbContext.FacilitatorUserData.AddAsync(newRecord, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
