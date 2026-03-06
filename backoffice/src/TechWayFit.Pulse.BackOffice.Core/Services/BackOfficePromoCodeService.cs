using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;
using TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficePromoCodeService : IBackOfficePromoCodeService
{
    private readonly BackOfficeMariaDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly ILogger<BackOfficePromoCodeService> _logger;

    public BackOfficePromoCodeService(
        BackOfficeMariaDbContext db,
        IAuditLogService audit,
        ILogger<BackOfficePromoCodeService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<PromoCodeSearchResult> SearchPromoCodesAsync(
      PromoCodeSearchQuery query,
      CancellationToken ct = default)
    {
        var q = _db.PromoCodes.AsNoTracking();

        if (query.IsActive.HasValue)
            q = q.Where(p => p.IsActive == query.IsActive.Value);

        if (query.IsExpired.HasValue)
        {
            var currentTime = DateTimeOffset.UtcNow;
            if (query.IsExpired.Value)
                q = q.Where(p => p.ValidUntil < currentTime);
            else
                q = q.Where(p => p.ValidUntil >= currentTime);
        }

        var totalCount = await q.CountAsync(ct);

        var promoCodes = await q
       .OrderByDescending(p => p.CreatedAt)
          .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
   .ToListAsync(ct);

        if (!promoCodes.Any())
        {
            return new PromoCodeSearchResult(new List<PromoCodeSummary>(), totalCount, query.Page, query.PageSize);
        }

        // Get plan display names - use JOIN approach to avoid Contains() type mapping issue
        var planIds = promoCodes.Select(p => p.TargetPlanId).Distinct().ToList();
var planDict = new Dictionary<Guid, string>();
        
     foreach (var planId in planIds)
        {
            var plan = await _db.SubscriptionPlans
      .AsNoTracking()
          .FirstOrDefaultAsync(p => p.Id == planId, ct);
            
if (plan != null)
            {
          planDict[planId] = plan.DisplayName;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var items = promoCodes.Select(p => new PromoCodeSummary(
     p.Id,
p.Code,
          planDict.GetValueOrDefault(p.TargetPlanId) ?? "Unknown Plan",
       p.DurationDays,
            p.MaxRedemptions,
     p.RedemptionsUsed,
            p.ValidFrom,
            p.ValidUntil,
   p.IsActive,
          GetPromoStatus(p, now))).ToList();

        return new PromoCodeSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<PromoCodeDetail?> GetPromoCodeDetailAsync(Guid id, CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes
         .AsNoTracking()
                 .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (promo is null) return null;

        var plan = await _db.SubscriptionPlans
           .AsNoTracking()
     .FirstOrDefaultAsync(p => p.Id == promo.TargetPlanId, ct);

        // Get recent redemptions (last 20)
        var redemptions = await _db.PromoCodeRedemptions
          .AsNoTracking()
   .Where(r => r.PromoCodeId == id)
      .OrderByDescending(r => r.RedeemedAt)
        .Take(20)
        .ToListAsync(ct);

        var redemptionSummaries = new List<PromoCodeRedemptionSummary>();
        foreach (var redemption in redemptions)
        {
            var user = await _db.FacilitatorUsers
 .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == redemption.FacilitatorUserId, ct);

            if (user != null)
            {
                redemptionSummaries.Add(new PromoCodeRedemptionSummary(
                    redemption.Id,
                   redemption.FacilitatorUserId,
          user.Email,
             redemption.SubscriptionId,
              redemption.RedeemedAt,
             redemption.IpAddress));
            }
        }

        return new PromoCodeDetail(
  promo.Id,
            promo.Code,
   promo.TargetPlanId,
            plan?.PlanCode ?? "unknown",
         plan?.DisplayName ?? "Unknown Plan",
            promo.DurationDays,
            promo.MaxRedemptions,
        promo.RedemptionsUsed,
            promo.ValidFrom,
  promo.ValidUntil,
promo.IsActive,
     promo.CreatedAt,
            promo.UpdatedAt,
            redemptionSummaries);
    }

    public async Task<PromoCodeDetail> CreatePromoCodeAsync(
     CreatePromoCodeRequest request,
        string operatorId,
        string operatorRole,
        string ipAddress,
     CancellationToken ct = default)
    {
        // Validate unique code
        var exists = await _db.PromoCodes
         .AnyAsync(p => p.Code == request.Code.Trim().ToUpperInvariant(), ct);

        if (exists)
            throw new InvalidOperationException($"Promo code '{request.Code}' already exists.");

        // Validate target plan exists
        var plan = await _db.SubscriptionPlans.FindAsync(new object[] { request.TargetPlanId }, ct);
        if (plan is null)
            throw new InvalidOperationException($"Target plan {request.TargetPlanId} not found.");

        var promo = new I.PromoCodeRecord
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpperInvariant(),
            TargetPlanId = request.TargetPlanId,
            DurationDays = request.DurationDays,
            MaxRedemptions = request.MaxRedemptions,
            RedemptionsUsed = 0,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.PromoCodes.Add(promo);

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
          "CreatePromoCode", "PromoCode", promo.Id.ToString(),
            null, null, promo.Code, "New promo code created", ipAddress,
            DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Promo code {Code} created by {OperatorId}", promo.Code, operatorId);

        return new PromoCodeDetail(
     promo.Id, promo.Code, promo.TargetPlanId, plan.PlanCode, plan.DisplayName,
            promo.DurationDays, promo.MaxRedemptions, promo.RedemptionsUsed,
            promo.ValidFrom, promo.ValidUntil, promo.IsActive,
    promo.CreatedAt, promo.UpdatedAt, new List<PromoCodeRedemptionSummary>());
    }

    public async Task UpdatePromoCodeAsync(
        UpdatePromoCodeRequest request,
    string operatorId,
        string operatorRole,
        string ipAddress,
    CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes.FindAsync(new object[] { request.Id }, ct)
   ?? throw new KeyNotFoundException($"Promo code {request.Id} not found.");

        // Check if code already used - if so, don't allow target plan or duration changes
        if (promo.RedemptionsUsed > 0 && (promo.TargetPlanId != request.TargetPlanId || promo.DurationDays != request.DurationDays))
        {
            throw new InvalidOperationException(
                  "Cannot change target plan or duration for promo codes that have been redeemed. " +
             "Create a new promo code instead.");
        }

        // Validate unique code if changed
        if (promo.Code != request.Code.Trim().ToUpperInvariant())
        {
            var exists = await _db.PromoCodes
          .AnyAsync(p => p.Code == request.Code.Trim().ToUpperInvariant() && p.Id != request.Id, ct);
            if (exists)
                throw new InvalidOperationException($"Promo code '{request.Code}' already exists.");
        }

        // Validate target plan exists
        var planExists = await _db.SubscriptionPlans.AnyAsync(p => p.Id == request.TargetPlanId, ct);
        if (!planExists)
            throw new InvalidOperationException($"Target plan {request.TargetPlanId} not found.");

        var changes = new List<(string Field, string OldValue, string NewValue)>();

        // Track changes
        if (promo.Code != request.Code.Trim().ToUpperInvariant())
            changes.Add(("Code", promo.Code, request.Code.Trim().ToUpperInvariant()));

        if (promo.TargetPlanId != request.TargetPlanId)
            changes.Add(("TargetPlanId", promo.TargetPlanId.ToString(), request.TargetPlanId.ToString()));

        if (promo.DurationDays != request.DurationDays)
            changes.Add(("DurationDays", promo.DurationDays.ToString(), request.DurationDays.ToString()));

        if (promo.MaxRedemptions != request.MaxRedemptions)
            changes.Add(("MaxRedemptions",
            promo.MaxRedemptions?.ToString() ?? "unlimited",
               request.MaxRedemptions?.ToString() ?? "unlimited"));

        if (promo.ValidFrom != request.ValidFrom)
            changes.Add(("ValidFrom", promo.ValidFrom.ToString("O"), request.ValidFrom.ToString("O")));

        if (promo.ValidUntil != request.ValidUntil)
            changes.Add(("ValidUntil", promo.ValidUntil.ToString("O"), request.ValidUntil.ToString("O")));

        if (promo.IsActive != request.IsActive)
            changes.Add(("IsActive", promo.IsActive.ToString(), request.IsActive.ToString()));

        if (!changes.Any())
        {
            _logger.LogInformation("Promo code {Code} update requested by {OperatorId} but no changes detected",
      promo.Code, operatorId);
            return;
        }

        // Apply changes
        promo.Code = request.Code.Trim().ToUpperInvariant();
        promo.TargetPlanId = request.TargetPlanId;
        promo.DurationDays = request.DurationDays;
        promo.MaxRedemptions = request.MaxRedemptions;
        promo.ValidFrom = request.ValidFrom;
        promo.ValidUntil = request.ValidUntil;
        promo.IsActive = request.IsActive;
        promo.UpdatedAt = DateTimeOffset.UtcNow;

        // Create audit record for each changed field
        foreach (var (field, oldValue, newValue) in changes)
        {
            await _audit.RecordAsync(new AuditLogEntry(
          Guid.NewGuid(), operatorId, operatorRole,
             "UpdatePromoCode", "PromoCode", promo.Id.ToString(),
         field, oldValue, newValue, request.Reason, ipAddress,
                 DateTimeOffset.UtcNow), ct);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Promo code {Code} updated by {OperatorId}", promo.Code, operatorId);
    }

    public async Task TogglePromoCodeActiveAsync(
        Guid id,
  bool isActive,
        string reason,
        string operatorId,
      string operatorRole,
        string ipAddress,
        CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes.FindAsync(new object[] { id }, ct)
      ?? throw new KeyNotFoundException($"Promo code {id} not found.");

        var oldValue = promo.IsActive.ToString();
        promo.IsActive = isActive;
        promo.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
    Guid.NewGuid(), operatorId, operatorRole,
            isActive ? "ActivatePromoCode" : "DeactivatePromoCode",
      "PromoCode", promo.Id.ToString(),
     "IsActive", oldValue, isActive.ToString(), reason, ipAddress,
  DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Promo code {Code} {Action} by {OperatorId}",
            promo.Code, isActive ? "activated" : "deactivated", operatorId);
    }

    public async Task DeletePromoCodeAsync(
     Guid id,
        string reason,
        string operatorId,
string operatorRole,
        string ipAddress,
        CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes.FindAsync(new object[] { id }, ct)
         ?? throw new KeyNotFoundException($"Promo code {id} not found.");

        // Check if code was ever redeemed
        var hasRedemptions = await _db.PromoCodeRedemptions
     .AnyAsync(r => r.PromoCodeId == id, ct);

        if (hasRedemptions)
        {
            throw new InvalidOperationException(
             $"Cannot delete promo code '{promo.Code}' that has been redeemed. " +
        "Deactivate it instead to prevent future use.");
        }

        var code = promo.Code;
        _db.PromoCodes.Remove(promo);

        await _audit.RecordAsync(new AuditLogEntry(
               Guid.NewGuid(), operatorId, operatorRole,
           "DeletePromoCode", "PromoCode", promo.Id.ToString(),
       null, code, null, reason, ipAddress,
      DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Promo code {Code} deleted by {OperatorId}", code, operatorId);
    }

    public async Task<IReadOnlyList<PromoCodeRedemptionSummary>> GetRedemptionHistoryAsync(
        Guid promoCodeId,
        CancellationToken ct = default)
    {
        var redemptions = await _db.PromoCodeRedemptions
                  .AsNoTracking()
                  .Where(r => r.PromoCodeId == promoCodeId)
                  .OrderByDescending(r => r.RedeemedAt)
                  .Take(100) // Limit to last 100 redemptions
           .ToListAsync(ct);

        var summaries = new List<PromoCodeRedemptionSummary>();
        foreach (var redemption in redemptions)
        {
            var user = await _db.FacilitatorUsers
                .AsNoTracking()
     .FirstOrDefaultAsync(u => u.Id == redemption.FacilitatorUserId, ct);

            if (user != null)
            {
                summaries.Add(new PromoCodeRedemptionSummary(
                redemption.Id,
             redemption.FacilitatorUserId,
             user.Email,
                redemption.SubscriptionId,
         redemption.RedeemedAt,
                       redemption.IpAddress));
            }
        }

        return summaries;
    }

    public async Task<(int TotalRedemptions, int UniqueUsers)> GetRedemptionStatsAsync(
      Guid promoCodeId,
        CancellationToken ct = default)
    {
        var redemptions = await _db.PromoCodeRedemptions
             .AsNoTracking()
                   .Where(r => r.PromoCodeId == promoCodeId)
              .ToListAsync(ct);

        return (redemptions.Count, redemptions.Select(r => r.FacilitatorUserId).Distinct().Count());
    }

    // ?? Helper Methods ????????????????????????????????????????????????????????

    private static string GetPromoStatus(I.PromoCodeRecord promo, DateTimeOffset statusCheckTime)
    {
        if (!promo.IsActive)
            return "Inactive";

        if (statusCheckTime < promo.ValidFrom)
            return "Not Yet Active";

        if (statusCheckTime > promo.ValidUntil)
            return "Expired";

        if (promo.MaxRedemptions.HasValue && promo.RedemptionsUsed >= promo.MaxRedemptions.Value)
            return "Limit Reached";

        return "Active";
    }
}
