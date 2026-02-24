using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository for FacilitatorUser with shared implementation and virtual methods for provider-specific behavior.
/// </summary>
public abstract class FacilitatorUserRepositoryBase<TContext> : IFacilitatorUserRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    protected FacilitatorUserRepositoryBase(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<FacilitatorUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.FacilitatorUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<FacilitatorUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.FacilitatorUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public virtual async Task<IReadOnlyList<FacilitatorUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.FacilitatorUsers
            .AsNoTracking();

        query = ApplySorting(query);

        var records = await query.ToListAsync(cancellationToken);
        return records.Select(MapToDomain).ToList();
    }

    public async Task AddAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(user);
        await dbContext.FacilitatorUsers.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(user);
        dbContext.FacilitatorUsers.Update(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.FacilitatorUsers
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    protected virtual IQueryable<FacilitatorUserRecord> ApplySorting(IQueryable<FacilitatorUserRecord> query)
    {
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
