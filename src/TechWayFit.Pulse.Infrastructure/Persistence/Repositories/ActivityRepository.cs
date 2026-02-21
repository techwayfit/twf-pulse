using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared ActivityRepository implementation - can be inherited by provider-specific implementations.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    protected readonly IPulseDbContext _dbContext;

    public ActivityRepository(IPulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Activities
         .AsNoTracking()
         .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Activity>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Activities
     .AsNoTracking()
    .Where(x => x.SessionId == sessionId)
        .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        return records.Select(record => record.ToDomain()).ToList();
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
 _dbContext.Activities.Add(activity.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Activities.FindAsync(new object[] { activity.Id }, cancellationToken);
        if (record is null)
        {
    throw new InvalidOperationException($"Activity with ID {activity.Id} not found.");
        }

// Update properties
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

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

  public async Task DeleteAsync(Activity activity, CancellationToken cancellationToken = default)
    {
   _dbContext.Activities.Remove(activity.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
}
}
