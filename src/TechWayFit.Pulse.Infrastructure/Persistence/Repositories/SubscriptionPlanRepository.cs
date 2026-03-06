using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB repository for subscription plans (read-only for main app)
/// </summary>
public sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
 private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

    public SubscriptionPlanRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
   _dbContextFactory = dbContextFactory;
    }

 private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

  public async Task<SubscriptionPlan?> GetByCodeAsync(string planCode, CancellationToken cancellationToken = default)
    {
   await using var dbContext = await CreateDbContextAsync(cancellationToken);
    var record = await dbContext.SubscriptionPlans
 .AsNoTracking()
   .FirstOrDefaultAsync(x => x.PlanCode == planCode, cancellationToken);

        return record == null ? null : new SubscriptionPlan(
        record.Id,
      record.PlanCode,
     record.DisplayName,
            record.Description,
            record.PriceMonthly,
   record.PriceYearly,
            record.MaxSessionsPerMonth,
            record.FeaturesJson,
        record.IsActive,
       record.SortOrder);
  }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
      var record = await dbContext.SubscriptionPlans
         .AsNoTracking()
  .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record == null ? null : new SubscriptionPlan(
       record.Id,
            record.PlanCode,
     record.DisplayName,
      record.Description,
            record.PriceMonthly,
 record.PriceYearly,
         record.MaxSessionsPerMonth,
          record.FeaturesJson,
    record.IsActive,
            record.SortOrder);
    }

    public async Task<IReadOnlyList<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
 var records = await dbContext.SubscriptionPlans
  .AsNoTracking()
   .Where(x => x.IsActive)
       .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PlanCode)
        .ToListAsync(cancellationToken);

        return records.Select(r => new SubscriptionPlan(
            r.Id,
  r.PlanCode,
 r.DisplayName,
            r.Description,
   r.PriceMonthly,
  r.PriceYearly,
   r.MaxSessionsPerMonth,
  r.FeaturesJson,
            r.IsActive,
  r.SortOrder)).ToList();
    }
}
