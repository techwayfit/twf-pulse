using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB repository for facilitator subscriptions
/// </summary>
public sealed class FacilitatorSubscriptionRepository : IFacilitatorSubscriptionRepository
{
    private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

 public FacilitatorSubscriptionRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
 _dbContextFactory = dbContextFactory;
 }

    private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

  public async Task<FacilitatorSubscription?> GetActiveSubscriptionAsync(
     Guid userId,
      CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
      var record = await dbContext.FacilitatorSubscriptions
 .AsNoTracking()
        .FirstOrDefaultAsync(x => x.FacilitatorUserId == userId && x.Status == "Active", cancellationToken);

        return record?.ToDomain();
    }

    public async Task<FacilitatorSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
await using var dbContext = await CreateDbContextAsync(cancellationToken);
     var record = await dbContext.FacilitatorSubscriptions
            .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<FacilitatorSubscription>> GetUserSubscriptionsAsync(
        Guid userId,
  CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
     var records = await dbContext.FacilitatorSubscriptions
          .AsNoTracking()
      .Where(x => x.FacilitatorUserId == userId)
  .OrderByDescending(x => x.CreatedAt)
   .ToListAsync(cancellationToken);

    return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task AddAsync(FacilitatorSubscription subscription, CancellationToken cancellationToken = default)
    {
     await using var dbContext = await CreateDbContextAsync(cancellationToken);
 dbContext.FacilitatorSubscriptions.Add(subscription.ToRecord());
 await dbContext.SaveChangesAsync(cancellationToken);
    }

  public async Task UpdateAsync(FacilitatorSubscription subscription, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.FacilitatorSubscriptions.FindAsync(
    new object[] { subscription.Id }, cancellationToken);
        
   if (record is null)
     {
throw new InvalidOperationException($"Subscription with ID {subscription.Id} not found.");
        }

  // Update all mutable fields
        record.PlanId = subscription.PlanId;
        record.Status = subscription.Status.ToString();
   record.ExpiresAt = subscription.ExpiresAt;
        record.CanceledAt = subscription.CanceledAt;
  record.SessionsUsed = subscription.SessionsUsed;
        record.SessionsResetAt = subscription.SessionsResetAt;
 record.PaymentProvider = subscription.PaymentProvider;
 record.ExternalCustomerId = subscription.ExternalCustomerId;
   record.ExternalSubscriptionId = subscription.ExternalSubscriptionId;
   record.UpdatedAt = subscription.UpdatedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
 }

    public async Task<IReadOnlyList<FacilitatorSubscription>> GetExpiredSubscriptionsAsync(
        DateTimeOffset before,
        CancellationToken cancellationToken = default)
    {
      await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.FacilitatorSubscriptions
   .AsNoTracking()
      .Where(x => x.ExpiresAt.HasValue && x.ExpiresAt.Value < before && x.Status == "Active")
   .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }
}

// ?? Mapping helpers (internal to this namespace) ??????????????????????????????
file static class FacilitatorSubscriptionMapper
{
 internal static FacilitatorSubscription ToDomain(this Infrastructure.Persistence.Entities.FacilitatorSubscriptionRecord r)
        => new(
      r.Id,
 r.FacilitatorUserId,
         r.PlanId,
            Enum.Parse<SubscriptionStatus>(r.Status),
            r.StartsAt,
 r.ExpiresAt,
            r.SessionsUsed,
        r.SessionsResetAt,
r.PaymentProvider,
 r.ExternalCustomerId,
r.ExternalSubscriptionId,
            r.CreatedAt,
   r.UpdatedAt);

    internal static Infrastructure.Persistence.Entities.FacilitatorSubscriptionRecord ToRecord(this FacilitatorSubscription s)
        => new()
  {
Id = s.Id,
     FacilitatorUserId = s.FacilitatorUserId,
   PlanId = s.PlanId,
     Status = s.Status.ToString(),
StartsAt = s.StartsAt,
            ExpiresAt = s.ExpiresAt,
       CanceledAt = s.CanceledAt,
      SessionsUsed = s.SessionsUsed,
  SessionsResetAt = s.SessionsResetAt,
            PaymentProvider = s.PaymentProvider,
            ExternalCustomerId = s.ExternalCustomerId,
 ExternalSubscriptionId = s.ExternalSubscriptionId,
CreatedAt = s.CreatedAt,
  UpdatedAt = s.UpdatedAt
        };
}
