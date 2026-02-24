using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared ActivityRepository implementation for all providers.
/// </summary>
public class ActivityRepository<TContext> : IActivityRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    public ActivityRepository(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Activities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Activity>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Activities
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        return records.Select(record => record.ToDomain()).ToList();
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.Activities.Add(activity.ToRecord());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Activities.FindAsync(new object[] { activity.Id }, cancellationToken);
        if (record is null)
        {
            throw new InvalidOperationException($"Activity with ID {activity.Id} not found.");
        }

        record.SessionId = activity.SessionId;
        record.Order = activity.Order;
        record.Type = (int)activity.Type;
        record.Title = activity.Title;
        record.Prompt = activity.Prompt;
        record.ConfigJson = activity.Config;
        record.Status = (int)activity.Status;
        record.OpenedAt = activity.OpenedAt;
        record.ClosedAt = activity.ClosedAt;
        record.DurationMinutes = activity.DurationMinutes;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.Activities.Remove(activity.ToRecord());
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
