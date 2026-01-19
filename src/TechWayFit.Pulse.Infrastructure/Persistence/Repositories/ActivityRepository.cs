using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

public sealed class ActivityRepository : IActivityRepository
{
    private readonly PulseDbContext _dbContext;

    public ActivityRepository(PulseDbContext dbContext)
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
        _dbContext.Activities.Update(activity.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        _dbContext.Activities.Remove(activity.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
