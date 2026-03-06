using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;
using TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficeSubscriptionService : IBackOfficeSubscriptionService
{
    private readonly BackOfficeMariaDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly ILogger<BackOfficeSubscriptionService> _logger;

    public BackOfficeSubscriptionService(
        BackOfficeMariaDbContext db,
      IAuditLogService audit,
ILogger<BackOfficeSubscriptionService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<SubscriptionSearchResult> SearchSubscriptionsAsync(
        SubscriptionSearchQuery query,
        CancellationToken ct = default)
    {
        var q = _db.FacilitatorSubscriptions.AsNoTracking();

        if (query.FacilitatorUserId.HasValue)
            q = q.Where(s => s.FacilitatorUserId == query.FacilitatorUserId.Value);
        if (!string.IsNullOrWhiteSpace(query.PlanCode))
        {
            var planList = await _db.SubscriptionPlans
      .AsNoTracking()
    .Where(p => p.PlanCode == query.PlanCode)
               .Select(p => p.Id)
               .ToArrayAsync(ct);
  q = q.Where(s => planList.Contains(s.PlanId));
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(s => s.Status == query.Status);

        var totalCount = await q.CountAsync(ct);

        var subscriptions = await q
             .OrderByDescending(s => s.CreatedAt)
           .Skip((query.Page - 1) * query.PageSize)
               .Take(query.PageSize)
                  .ToListAsync(ct);

        var facilitatorIds = subscriptions.Select(s => s.FacilitatorUserId).Distinct().ToArray();
        var facilitators = await _db.FacilitatorUsers
            .AsNoTracking()
            .Where(f => facilitatorIds.Contains(f.Id))
    .ToDictionaryAsync(f => f.Id, f => f.Email, ct);

        var planIds = subscriptions.Select(s => s.PlanId).Distinct().ToArray();
        var plans = await _db.SubscriptionPlans
        .AsNoTracking()
          .Where(p => planIds.Contains(p.Id))
  .ToDictionaryAsync(p => p.Id, p => (p.PlanCode, p.DisplayName, p.MaxSessionsPerMonth), ct);

        var items = subscriptions.Select(s =>
            {
                var (planCode, planName, sessionLimit) = plans.GetValueOrDefault(s.PlanId, ("unknown", "Unknown", 0));
                var email = facilitators.GetValueOrDefault(s.FacilitatorUserId, "unknown@example.com");

                return new FacilitatorSubscriptionSummary(
                  s.Id, s.FacilitatorUserId, email, planCode, planName,
                 s.Status, s.SessionsUsed, sessionLimit,
                  s.StartsAt, s.ExpiresAt, s.CanceledAt);
            }).ToList();

        return new SubscriptionSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<FacilitatorSubscriptionDetail?> GetSubscriptionDetailAsync(
     Guid subscriptionId,
 CancellationToken ct = default)
    {
        var sub = await _db.FacilitatorSubscriptions
        .AsNoTracking()
              .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);

        if (sub is null) return null;

        var facilitator = await _db.FacilitatorUsers
     .AsNoTracking()
   .FirstOrDefaultAsync(f => f.Id == sub.FacilitatorUserId, ct);

        var plan = await _db.SubscriptionPlans
      .AsNoTracking()
       .FirstOrDefaultAsync(p => p.Id == sub.PlanId, ct);

        return new FacilitatorSubscriptionDetail(
    sub.Id, sub.FacilitatorUserId, facilitator?.Email ?? "unknown",
            sub.PlanId, plan?.PlanCode ?? "unknown", plan?.DisplayName ?? "Unknown",
     sub.Status, sub.StartsAt, sub.ExpiresAt, sub.CanceledAt,
 sub.SessionsUsed, sub.SessionsResetAt,
sub.PaymentProvider, sub.ExternalCustomerId, sub.ExternalSubscriptionId,
            sub.CreatedAt, sub.UpdatedAt);
    }

    public async Task<IReadOnlyList<FacilitatorSubscriptionSummary>> GetUserSubscriptionHistoryAsync(
        Guid facilitatorUserId,
        CancellationToken ct = default)
    {
        var subscriptions = await _db.FacilitatorSubscriptions
            .AsNoTracking()
  .Where(s => s.FacilitatorUserId == facilitatorUserId)
            .OrderByDescending(s => s.CreatedAt)
    .ToListAsync(ct);

        var facilitator = await _db.FacilitatorUsers
    .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == facilitatorUserId, ct);

        var planIds = subscriptions.Select(s => s.PlanId).Distinct().ToArray();
        var plans = await _db.SubscriptionPlans
   .AsNoTracking()
       .Where(p => planIds.Contains(p.Id))
  .ToDictionaryAsync(p => p.Id, p => (p.PlanCode, p.DisplayName, p.MaxSessionsPerMonth), ct);

        return subscriptions.Select(s =>
  {
      var (planCode, planName, sessionLimit) = plans.GetValueOrDefault(s.PlanId, ("unknown", "Unknown", 0));

      return new FacilitatorSubscriptionSummary(
s.Id, s.FacilitatorUserId, facilitator?.Email ?? "unknown",
  planCode, planName, s.Status, s.SessionsUsed, sessionLimit,
s.StartsAt, s.ExpiresAt, s.CanceledAt);
  }).ToList();
    }

    public async Task<FacilitatorSubscriptionDetail> AssignPlanAsync(
        AssignPlanRequest request,
 string operatorId,
    string operatorRole,
   string ipAddress,
        CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans
  .AsNoTracking()
  .FirstOrDefaultAsync(p => p.PlanCode == request.PlanCode, ct)
      ?? throw new InvalidOperationException($"Plan '{request.PlanCode}' not found.");

        var facilitator = await _db.FacilitatorUsers
  .AsNoTracking()
    .FirstOrDefaultAsync(f => f.Id == request.FacilitatorUserId, ct)
 ?? throw new InvalidOperationException($"User {request.FacilitatorUserId} not found.");

        // Expire old active subscriptions
        var oldSubs = await _db.FacilitatorSubscriptions
            .Where(s => s.FacilitatorUserId == request.FacilitatorUserId && s.Status == "Active")
    .ToListAsync(ct);

        foreach (var old in oldSubs)
        {
            old.Status = "Expired";
            old.ExpiresAt = DateTimeOffset.UtcNow;
            old.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Create new subscription
        var newSub = new I.FacilitatorSubscriptionRecord
        {
            Id = Guid.NewGuid(),
            FacilitatorUserId = request.FacilitatorUserId,
            PlanId = plan.Id,
            Status = request.Status,
            StartsAt = request.StartsAt,
            ExpiresAt = request.ExpiresAt,
            CanceledAt = null,
            SessionsUsed = 0,
            SessionsResetAt = DateTimeOffset.UtcNow.AddMonths(1),
            PaymentProvider = null,
            ExternalCustomerId = null,
            ExternalSubscriptionId = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.FacilitatorSubscriptions.Add(newSub);

        await _audit.RecordAsync(new AuditLogEntry(
                Guid.NewGuid(), operatorId, operatorRole,
             "AssignPlan", "FacilitatorSubscription", newSub.Id.ToString(),
            "PlanCode", null, plan.PlanCode, request.Reason, ipAddress,
                  DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Plan {PlanCode} assigned to user {UserId} by {OperatorId}",
             plan.PlanCode, facilitator.Email, operatorId);

        return new FacilitatorSubscriptionDetail(
     newSub.Id, newSub.FacilitatorUserId, facilitator.Email,
   newSub.PlanId, plan.PlanCode, plan.DisplayName,
newSub.Status, newSub.StartsAt, newSub.ExpiresAt, newSub.CanceledAt,
newSub.SessionsUsed, newSub.SessionsResetAt,
       null, null, null, newSub.CreatedAt, newSub.UpdatedAt);
    }

    public async Task CancelSubscriptionAsync(
   CancelSubscriptionRequest request,
     string operatorId,
       string operatorRole,
   string ipAddress,
           CancellationToken ct = default)
    {
        var sub = await _db.FacilitatorSubscriptions.FindAsync(new object[] { request.SubscriptionId }, ct)
   ?? throw new KeyNotFoundException($"Subscription {request.SubscriptionId} not found.");

        sub.Status = "Canceled";
        sub.CanceledAt = DateTimeOffset.UtcNow;
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
     Guid.NewGuid(), operatorId, operatorRole,
  "CancelSubscription", "FacilitatorSubscription", sub.Id.ToString(),
    "Status", "Active", "Canceled", request.Reason, ipAddress,
  DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Subscription {SubId} canceled by {OperatorId}", sub.Id, operatorId);
    }

    public async Task ResetQuotaAsync(
   ResetQuotaRequest request,
   string operatorId,
string operatorRole,
  string ipAddress,
CancellationToken ct = default)
    {
        var sub = await _db.FacilitatorSubscriptions.FindAsync(new object[] { request.SubscriptionId }, ct)
              ?? throw new KeyNotFoundException($"Subscription {request.SubscriptionId} not found.");

        var oldValue = sub.SessionsUsed.ToString();
        sub.SessionsUsed = 0;
        sub.SessionsResetAt = DateTimeOffset.UtcNow.AddMonths(1);
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
     "ResetQuota", "FacilitatorSubscription", sub.Id.ToString(),
       "SessionsUsed", oldValue, "0", request.Reason, ipAddress,
 DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Quota reset for subscription {SubId} by {OperatorId} - was {OldUsage}",
   sub.Id, operatorId, oldValue);
    }

    public async Task<(int Active, int Canceled, int Expired, int Trial)> GetSubscriptionStatusCountsAsync(
        CancellationToken ct = default)
    {
        var counts = await _db.FacilitatorSubscriptions
        .AsNoTracking()
                  .GroupBy(s => s.Status)
                  .Select(g => new { Status = g.Key, Count = g.Count() })
                  .ToListAsync(ct);

        var countMap = counts.ToDictionary(x => x.Status, x => x.Count);

        return (
                    countMap.GetValueOrDefault("Active"),
             countMap.GetValueOrDefault("Canceled"),
          countMap.GetValueOrDefault("Expired"),
                    countMap.GetValueOrDefault("Trial"));
    }
}
