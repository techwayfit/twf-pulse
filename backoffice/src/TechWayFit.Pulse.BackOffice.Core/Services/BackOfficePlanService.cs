using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;
using TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficePlanService : IBackOfficePlanService
{
    private readonly BackOfficeMariaDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly ILogger<BackOfficePlanService> _logger;

    public BackOfficePlanService(
   BackOfficeMariaDbContext db,
     IAuditLogService audit,
        ILogger<BackOfficePlanService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<PlanSearchResult> SearchPlansAsync(PlanSearchQuery query, CancellationToken ct = default)
    {
        var q = _db.SubscriptionPlans.AsNoTracking();

        if (query.IsActive.HasValue)
            q = q.Where(p => p.IsActive == query.IsActive.Value);

        var totalCount = await q.CountAsync(ct);

      var plans = await q
   .OrderBy(p => p.SortOrder)
    .ThenBy(p => p.PlanCode)
            .Skip((query.Page - 1) * query.PageSize)
       .Take(query.PageSize)
      .ToListAsync(ct);

        if (!plans.Any())
   {
 return new PlanSearchResult(new List<SubscriptionPlanSummary>(), totalCount, query.Page, query.PageSize);
        }

        // For small number of plans (max 5), sequential queries are perfectly fine
        // Avoids all EF Core translation issues with Contains/Join
   var countMap = new Dictionary<Guid, int>();
  foreach (var plan in plans)
        {
            var count = await _db.FacilitatorSubscriptions
    .AsNoTracking()
       .Where(s => s.PlanId == plan.Id && s.Status == "Active")
     .CountAsync(ct);
     countMap[plan.Id] = count;
        }

        var items = plans.Select(p => new SubscriptionPlanSummary(
          p.Id,
    p.PlanCode,
 p.DisplayName,
            p.PriceMonthly,
     p.MaxSessionsPerMonth,
            p.IsActive,
   p.SortOrder,
  countMap.GetValueOrDefault(p.Id))).ToList();

        return new PlanSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<SubscriptionPlanDetail?> GetPlanDetailAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans
             .AsNoTracking()
       .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return null;

        var activeCount = await _db.FacilitatorSubscriptions
     .AsNoTracking()
  .CountAsync(s => s.PlanId == planId && s.Status == "Active", ct);

        var totalCount = await _db.FacilitatorSubscriptions
        .AsNoTracking()
              .CountAsync(s => s.PlanId == planId, ct);

        return new SubscriptionPlanDetail(
                plan.Id,
                plan.PlanCode,
                plan.DisplayName,
                plan.Description,
                plan.PriceMonthly,
                plan.PriceYearly,
                plan.MaxSessionsPerMonth,
                plan.FeaturesJson,
                plan.IsActive,
                plan.SortOrder,
                plan.CreatedAt,
                plan.UpdatedAt,
                activeCount,
                totalCount);
    }

    public async Task<SubscriptionPlanDetail> CreatePlanAsync(
        CreateSubscriptionPlanRequest request,
  string operatorId,
      string operatorRole,
        string ipAddress,
        CancellationToken ct = default)
    {
        // Validate unique plan code
        var exists = await _db.SubscriptionPlans
       .AnyAsync(p => p.PlanCode == request.PlanCode, ct);

        if (exists)
            throw new InvalidOperationException($"Plan code '{request.PlanCode}' already exists.");

        var plan = new I.SubscriptionPlanRecord
        {
            Id = Guid.NewGuid(),
            PlanCode = request.PlanCode,
            DisplayName = request.DisplayName,
            Description = request.Description,
            PriceMonthly = request.PriceMonthly,
            PriceYearly = request.PriceYearly,
            MaxSessionsPerMonth = request.MaxSessionsPerMonth,
            FeaturesJson = request.FeaturesJson,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.SubscriptionPlans.Add(plan);

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
          "CreateSubscriptionPlan", "SubscriptionPlan", plan.Id.ToString(),
                null, null, plan.PlanCode, "New subscription plan created", ipAddress,
                    DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Plan {PlanCode} created by {OperatorId}", plan.PlanCode, operatorId);

        return new SubscriptionPlanDetail(
            plan.Id, plan.PlanCode, plan.DisplayName, plan.Description,
            plan.PriceMonthly, plan.PriceYearly, plan.MaxSessionsPerMonth,
            plan.FeaturesJson, plan.IsActive, plan.SortOrder,
            plan.CreatedAt, plan.UpdatedAt, 0, 0);
    }

    public async Task UpdatePlanAsync(
      UpdateSubscriptionPlanRequest request,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(new object[] { request.PlanId }, ct)
         ?? throw new KeyNotFoundException($"Plan {request.PlanId} not found.");

        var changes = new List<(string Field, string OldValue, string NewValue)>();

     // Track all field changes for audit
        if (plan.DisplayName != request.DisplayName)
            changes.Add(("DisplayName", plan.DisplayName, request.DisplayName));
     
        if (plan.Description != request.Description)
changes.Add(("Description", plan.Description ?? "", request.Description ?? ""));
   
  if (plan.PriceMonthly != request.PriceMonthly)
            changes.Add(("PriceMonthly", plan.PriceMonthly.ToString("F2"), request.PriceMonthly.ToString("F2")));
   
     if (plan.PriceYearly != request.PriceYearly)
            changes.Add(("PriceYearly", 
       plan.PriceYearly?.ToString("F2") ?? "null", 
    request.PriceYearly?.ToString("F2") ?? "null"));
     
   if (plan.MaxSessionsPerMonth != request.MaxSessionsPerMonth)
            changes.Add(("MaxSessionsPerMonth", 
 plan.MaxSessionsPerMonth.ToString(), 
        request.MaxSessionsPerMonth.ToString()));
  
        if (plan.FeaturesJson != request.FeaturesJson)
            changes.Add(("FeaturesJson", plan.FeaturesJson, request.FeaturesJson));
        
        if (plan.IsActive != request.IsActive)
   changes.Add(("IsActive", plan.IsActive.ToString(), request.IsActive.ToString()));

        if (plan.SortOrder != request.SortOrder)
        changes.Add(("SortOrder", plan.SortOrder.ToString(), request.SortOrder.ToString()));

        // If no changes detected, skip update
        if (!changes.Any())
      {
            _logger.LogInformation("Plan {PlanCode} update requested by {OperatorId} but no changes detected", 
             plan.PlanCode, operatorId);
   return;
        }

        // Apply changes
        plan.DisplayName = request.DisplayName;
        plan.Description = request.Description;
        plan.PriceMonthly = request.PriceMonthly;
     plan.PriceYearly = request.PriceYearly;
        plan.MaxSessionsPerMonth = request.MaxSessionsPerMonth;
        plan.FeaturesJson = request.FeaturesJson;
        plan.IsActive = request.IsActive;
      plan.SortOrder = request.SortOrder;
    plan.UpdatedAt = DateTimeOffset.UtcNow;

        // Create audit record for each changed field
        foreach (var (field, oldValue, newValue) in changes)
        {
      await _audit.RecordAsync(new AuditLogEntry(
          Guid.NewGuid(), operatorId, operatorRole,
   "UpdateSubscriptionPlan", "SubscriptionPlan", plan.Id.ToString(),
      field, oldValue, newValue, request.Reason, ipAddress,
    DateTimeOffset.UtcNow), ct);
        }

      await _db.SaveChangesAsync(ct);
    }

    public async Task TogglePlanActiveAsync(
        Guid planId,
     bool isActive,
     string reason,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(new object[] { planId }, ct)
    ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        var oldValue = plan.IsActive.ToString();
        plan.IsActive = isActive;
        plan.UpdatedAt = DateTimeOffset.UtcNow;

  await _audit.RecordAsync(new AuditLogEntry(
           Guid.NewGuid(), operatorId, operatorRole,
       isActive ? "ActivatePlan" : "DeactivatePlan",
         "SubscriptionPlan", plan.Id.ToString(),
     "IsActive", oldValue, isActive.ToString(), reason, ipAddress,
           DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Plan {PlanCode} {Action} by {OperatorId}",
 plan.PlanCode, isActive ? "activated" : "deactivated", operatorId);
    }

    public async Task<IReadOnlyList<SubscriptionPlanSummary>> GetAllActivePlansAsync(CancellationToken ct = default)
    {
  var plans = await _db.SubscriptionPlans
         .AsNoTracking()
 .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
  .ThenBy(p => p.PlanCode)
          .ToListAsync(ct);

        return plans.Select(p => new SubscriptionPlanSummary(
            p.Id,
          p.PlanCode,
   p.DisplayName,
        p.PriceMonthly,
   p.MaxSessionsPerMonth,
    p.IsActive,
     p.SortOrder,
     0 // Active subscription count not needed for simple selector
        )).ToList();
    }

    public async Task<IReadOnlyList<(string PlanCode, int UserCount, decimal MonthlyRevenue)>> GetRevenueSummaryAsync(
      CancellationToken ct = default)
    {
        // Use a single JOIN query to get plan details with subscription counts
        // Optimal for small number of plans (max 5)
  var revenueData = await (
            from s in _db.FacilitatorSubscriptions.AsNoTracking()
            where s.Status == "Active"
   join p in _db.SubscriptionPlans.AsNoTracking()
             on s.PlanId equals p.Id
        group s by new { p.Id, p.PlanCode, p.PriceMonthly } into g
     select new
            {
    PlanCode = g.Key.PlanCode,
UserCount = g.Count(),
      MonthlyRevenue = g.Key.PriceMonthly * g.Count()
            }
        ).ToListAsync(ct);

    return revenueData
          .Select(x => (x.PlanCode, x.UserCount, x.MonthlyRevenue))
       .OrderByDescending(x => x.MonthlyRevenue)
         .ToList();
    }
}
