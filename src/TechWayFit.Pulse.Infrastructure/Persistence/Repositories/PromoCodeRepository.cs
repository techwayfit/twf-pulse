using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB repository for promo code management
/// </summary>
public sealed class PromoCodeRepository : IPromoCodeRepository
{
    private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

    public PromoCodeRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
 {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
  }

    public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
 await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.PromoCodes
            .AsNoTracking()
 .FirstOrDefaultAsync(x => x.Code == code.Trim().ToUpperInvariant(), cancellationToken);

        return record?.ToDomain();
    }

    public async Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
   await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.PromoCodes
            .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<(IReadOnlyList<PromoCode> PromoCodes, int TotalCount)> GetAllAsync(
      bool? isActive = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var query = dbContext.PromoCodes.AsNoTracking();

        if (isActive.HasValue)
       query = query.Where(x => x.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
 .OrderByDescending(x => x.CreatedAt)
      .Skip((page - 1) * pageSize)
        .Take(pageSize)
  .ToListAsync(cancellationToken);

    return (records.Select(r => r.ToDomain()).ToList(), totalCount);
    }

  public async Task<bool> HasUserRedeemedAsync(
        Guid promoCodeId,
        Guid userId,
     CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.PromoCodeRedemptions
            .AsNoTracking()
       .AnyAsync(x => x.PromoCodeId == promoCodeId && x.FacilitatorUserId == userId, cancellationToken);
}

    public async Task<IReadOnlyList<PromoCodeRedemption>> GetRedemptionsAsync(
        Guid promoCodeId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.PromoCodeRedemptions
   .AsNoTracking()
     .Where(x => x.PromoCodeId == promoCodeId)
            .OrderByDescending(x => x.RedeemedAt)
  .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<PromoCodeRedemption>> GetUserRedemptionsAsync(
        Guid userId,
     CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.PromoCodeRedemptions
 .AsNoTracking()
    .Where(x => x.FacilitatorUserId == userId)
      .OrderByDescending(x => x.RedeemedAt)
     .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task RecordRedemptionAsync(
        Guid promoCodeId,
        Guid userId,
        Guid subscriptionId,
        string ipAddress,
        DateTimeOffset redeemedAt,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
var redemption = new PromoCodeRedemption(
    Guid.NewGuid(),
          promoCodeId,
    userId,
            subscriptionId,
            redeemedAt,
     ipAddress);

        dbContext.PromoCodeRedemptions.Add(redemption.ToRecord());
   await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
 dbContext.PromoCodes.Add(promoCode.ToRecord());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

  public async Task UpdateAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.PromoCodes.FindAsync(new object[] { promoCode.Id }, cancellationToken);
  if (record is null)
        {
        throw new InvalidOperationException($"Promo code with ID {promoCode.Id} not found.");
        }

    record.Code = promoCode.Code;
        record.TargetPlanId = promoCode.TargetPlanId;
        record.DurationDays = promoCode.DurationDays;
        record.MaxRedemptions = promoCode.MaxRedemptions;
   record.RedemptionsUsed = promoCode.RedemptionsUsed;
        record.ValidFrom = promoCode.ValidFrom;
      record.ValidUntil = promoCode.ValidUntil;
record.IsActive = promoCode.IsActive;
  record.UpdatedAt = promoCode.UpdatedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        
        // Check if code was ever redeemed
  var hasRedemptions = await dbContext.PromoCodeRedemptions
 .AnyAsync(x => x.PromoCodeId == id, cancellationToken);
        
        if (hasRedemptions)
    throw new InvalidOperationException("Cannot delete promo code that has been redeemed. Deactivate it instead.");

    var record = await dbContext.PromoCodes.FindAsync(new object[] { id }, cancellationToken);
        if (record != null)
   {
            dbContext.PromoCodes.Remove(record);
      await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

// ?? Mapping helpers (internal to this namespace) ??????????????????????????????
file static class PromoCodeMapper
{
    internal static PromoCode ToDomain(this PromoCodeRecord r)
        => new(r.Id, r.Code, r.TargetPlanId, r.DurationDays, r.MaxRedemptions,
        r.RedemptionsUsed, r.ValidFrom, r.ValidUntil, r.IsActive, r.CreatedAt, r.UpdatedAt);

    internal static PromoCodeRecord ToRecord(this PromoCode p)
  => new()
 {
            Id = p.Id,
   Code = p.Code,
   TargetPlanId = p.TargetPlanId,
            DurationDays = p.DurationDays,
     MaxRedemptions = p.MaxRedemptions,
            RedemptionsUsed = p.RedemptionsUsed,
   ValidFrom = p.ValidFrom,
       ValidUntil = p.ValidUntil,
            IsActive = p.IsActive,
CreatedAt = p.CreatedAt,
         UpdatedAt = p.UpdatedAt
        };

    internal static PromoCodeRedemption ToDomain(this PromoCodeRedemptionRecord r)
        => new(r.Id, r.PromoCodeId, r.FacilitatorUserId, r.SubscriptionId, r.RedeemedAt, r.IpAddress);

    internal static PromoCodeRedemptionRecord ToRecord(this PromoCodeRedemption p)
        => new()
        {
       Id = p.Id,
      PromoCodeId = p.PromoCodeId,
        FacilitatorUserId = p.FacilitatorUserId,
            SubscriptionId = p.SubscriptionId,
            RedeemedAt = p.RedeemedAt,
       IpAddress = p.IpAddress
        };
}
