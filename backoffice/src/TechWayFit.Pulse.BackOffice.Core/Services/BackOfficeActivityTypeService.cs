using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;
using TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;
using TechWayFit.Pulse.Domain.Enums;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficeActivityTypeService : IBackOfficeActivityTypeService
{
 private readonly BackOfficeMariaDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly ILogger<BackOfficeActivityTypeService> _logger;

    public BackOfficeActivityTypeService(
   BackOfficeMariaDbContext db,
        IAuditLogService audit,
        ILogger<BackOfficeActivityTypeService> logger)
    {
     _db = db;
        _audit = audit;
     _logger = logger;
    }

    public async Task<ActivityTypeSearchResult> SearchActivityTypesAsync(
        ActivityTypeSearchQuery query,
      CancellationToken ct = default)
    {
  var q = _db.ActivityTypeDefinitions.AsNoTracking();

     if (query.IsActive.HasValue)
      q = q.Where(a => a.IsActive == query.IsActive.Value);
        if (query.RequiresPremium.HasValue)
            q = q.Where(a => a.RequiresPremium == query.RequiresPremium.Value);

        var totalCount = await q.CountAsync(ct);

        var types = await q
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.ActivityType)
     .Skip((query.Page - 1) * query.PageSize)
     .Take(query.PageSize)
   .ToListAsync(ct);

        var items = types.Select(t => new ActivityTypeDefinitionSummary(
          t.Id,
  t.ActivityType,
  ((ActivityType)t.ActivityType).ToString(),
      t.DisplayName,
 t.IconClass,
     t.ColorHex,
      t.RequiresPremium,
  t.ApplicablePlanIds,
  t.IsAvailableToAllPlans,
    t.IsActive,
   t.SortOrder)).ToList();

    return new ActivityTypeSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<ActivityTypeDefinitionDetail?> GetActivityTypeDetailAsync(
  Guid id,
      CancellationToken ct = default)
    {
        var type = await _db.ActivityTypeDefinitions
            .AsNoTracking()
 .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (type is null) return null;

        return new ActivityTypeDefinitionDetail(
    type.Id,
   type.ActivityType,
            ((ActivityType)type.ActivityType).ToString(),
       type.DisplayName,
     type.Description,
         type.IconClass,
      type.ColorHex,
   type.RequiresPremium,
      type.ApplicablePlanIds,
            type.IsAvailableToAllPlans,
      type.IsActive,
      type.SortOrder,
      type.CreatedAt,
    type.UpdatedAt);
    }

    public async Task<ActivityTypeDefinitionDetail> CreateActivityTypeAsync(
 CreateActivityTypeDefinitionRequest request,
  string operatorId,
        string operatorRole,
      string ipAddress,
        CancellationToken ct = default)
    {
    var exists = await _db.ActivityTypeDefinitions
          .AnyAsync(t => t.ActivityType == request.ActivityType, ct);

        if (exists)
            throw new InvalidOperationException($"Activity type {request.ActivityType} already defined.");

        var type = new I.ActivityTypeDefinitionRecord
        {
    Id = Guid.NewGuid(),
      ActivityType = request.ActivityType,
            DisplayName = request.DisplayName,
        Description = request.Description,
  IconClass = request.IconClass,
            ColorHex = request.ColorHex,
      RequiresPremium = request.RequiresPremium,
            ApplicablePlanIds = request.ApplicablePlanIds,
            IsAvailableToAllPlans = request.IsAvailableToAllPlans,
    IsActive = request.IsActive,
            SortOrder = request.SortOrder,
     CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.ActivityTypeDefinitions.Add(type);

        await _audit.RecordAsync(new AuditLogEntry(
      Guid.NewGuid(), operatorId, operatorRole,
            "CreateActivityType", "ActivityTypeDefinition", type.Id.ToString(),
            null, null, type.DisplayName, "New activity type definition created", ipAddress,
 DateTimeOffset.UtcNow), ct);

await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Activity type {TypeName} created by {OperatorId}", 
     ((ActivityType)type.ActivityType).ToString(), operatorId);

     return new ActivityTypeDefinitionDetail(
      type.Id, type.ActivityType, ((ActivityType)type.ActivityType).ToString(),
        type.DisplayName, type.Description, type.IconClass, type.ColorHex,
      type.RequiresPremium, type.ApplicablePlanIds, type.IsAvailableToAllPlans, type.IsActive, type.SortOrder,
      type.CreatedAt, type.UpdatedAt);
    }

    public async Task UpdateActivityTypeAsync(
     UpdateActivityTypeDefinitionRequest request,
  string operatorId,
        string operatorRole,
      string ipAddress,
        CancellationToken ct = default)
    {
        var type = await _db.ActivityTypeDefinitions.FindAsync(new object[] { request.Id }, ct)
    ?? throw new KeyNotFoundException($"Activity type {request.Id} not found.");

   var changes = new List<(string Field, string OldValue, string NewValue)>();

        // Track all field changes for comprehensive audit trail
     if (type.DisplayName != request.DisplayName)
         changes.Add(("DisplayName", type.DisplayName, request.DisplayName));
        
        if (type.Description != request.Description)
    changes.Add(("Description", type.Description, request.Description));
        
        if (type.IconClass != request.IconClass)
         changes.Add(("IconClass", type.IconClass, request.IconClass));
        
        if (type.ColorHex != request.ColorHex)
            changes.Add(("ColorHex", type.ColorHex, request.ColorHex));
        
        if (type.RequiresPremium != request.RequiresPremium)
 changes.Add(("RequiresPremium", type.RequiresPremium.ToString(), request.RequiresPremium.ToString()));
        
        if (type.ApplicablePlanIds != request.ApplicablePlanIds)
 changes.Add(("ApplicablePlanIds", type.ApplicablePlanIds ?? "NULL", request.ApplicablePlanIds ?? "NULL"));
   
        if (type.IsAvailableToAllPlans != request.IsAvailableToAllPlans)
            changes.Add(("IsAvailableToAllPlans", type.IsAvailableToAllPlans.ToString(), request.IsAvailableToAllPlans.ToString()));
   
        if (type.IsActive != request.IsActive)
      changes.Add(("IsActive", type.IsActive.ToString(), request.IsActive.ToString()));
    
if (type.SortOrder != request.SortOrder)
      changes.Add(("SortOrder", type.SortOrder.ToString(), request.SortOrder.ToString()));

        // If no changes detected, skip update
   if (!changes.Any())
        {
      _logger.LogInformation("Activity type {TypeName} update requested by {OperatorId} but no changes detected",
            ((ActivityType)type.ActivityType).ToString(), operatorId);
      return;
   }

  // Apply all changes
        type.DisplayName = request.DisplayName;
        type.Description = request.Description;
        type.IconClass = request.IconClass;
     type.ColorHex = request.ColorHex;
      type.RequiresPremium = request.RequiresPremium;
        type.ApplicablePlanIds = request.ApplicablePlanIds;
  type.IsAvailableToAllPlans = request.IsAvailableToAllPlans;
    type.IsActive = request.IsActive;
    type.SortOrder = request.SortOrder;
    type.UpdatedAt = DateTimeOffset.UtcNow;

        // Create audit record for each changed field
 foreach (var (field, oldValue, newValue) in changes)
    {
      await _audit.RecordAsync(new AuditLogEntry(
    Guid.NewGuid(), operatorId, operatorRole,
       "UpdateActivityType", "ActivityTypeDefinition", type.Id.ToString(),
     field, oldValue, newValue, request.Reason, ipAddress,
    DateTimeOffset.UtcNow), ct);
        }

      await _db.SaveChangesAsync(ct);

      _logger.LogInformation("Activity type {TypeName} updated by {OperatorId} - {ChangeCount} fields changed",
  ((ActivityType)type.ActivityType).ToString(), operatorId, changes.Count);
    }

    public async Task TogglePremiumAsync(
        TogglePremiumRequest request,
        string operatorId,
        string operatorRole,
     string ipAddress,
     CancellationToken ct = default)
    {
        var type = await _db.ActivityTypeDefinitions.FindAsync(new object[] { request.Id }, ct)
          ?? throw new KeyNotFoundException($"Activity type {request.Id} not found.");

      var changes = new List<(string Field, string OldValue, string NewValue)>();
        
 // Track RequiresPremium change
 if (type.RequiresPremium != request.RequiresPremium)
      changes.Add(("RequiresPremium", type.RequiresPremium.ToString(), request.RequiresPremium.ToString()));
        
   // Track ApplicablePlanIds change
   if (type.ApplicablePlanIds != request.ApplicablePlanIds)
       changes.Add(("ApplicablePlanIds", type.ApplicablePlanIds ?? "NULL", request.ApplicablePlanIds ?? "NULL"));
    
        // Track IsAvailableToAllPlans change
 if (type.IsAvailableToAllPlans != request.IsAvailableToAllPlans)
        changes.Add(("IsAvailableToAllPlans", type.IsAvailableToAllPlans.ToString(), request.IsAvailableToAllPlans.ToString()));

    // Apply changes
   type.RequiresPremium = request.RequiresPremium;
   type.ApplicablePlanIds = request.ApplicablePlanIds;
type.IsAvailableToAllPlans = request.IsAvailableToAllPlans;
     type.UpdatedAt = DateTimeOffset.UtcNow;

        // Audit each field change
        foreach (var (field, oldValue, newValue) in changes)
     {
       await _audit.RecordAsync(new AuditLogEntry(
       Guid.NewGuid(), operatorId, operatorRole,
      request.RequiresPremium ? "MakeActivityPremium" : "MakeActivityFree",
       "ActivityTypeDefinition", type.Id.ToString(),
       field, oldValue, newValue, request.Reason, ipAddress,
       DateTimeOffset.UtcNow), ct);
  }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Activity type {TypeName} {Action} by {OperatorId} - {ChangeCount} fields changed",
 ((ActivityType)type.ActivityType).ToString(), 
   request.RequiresPremium ? "made premium" : "made free", operatorId, changes.Count);
    }

    public async Task ToggleActiveAsync(
        Guid id,
 bool isActive,
        string reason,
        string operatorId,
 string operatorRole,
        string ipAddress,
        CancellationToken ct = default)
    {
        var type = await _db.ActivityTypeDefinitions.FindAsync(new object[] { id }, ct)
      ?? throw new KeyNotFoundException($"Activity type {id} not found.");

  var oldValue = type.IsActive.ToString();
        type.IsActive = isActive;
        type.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
    Guid.NewGuid(), operatorId, operatorRole,
          isActive ? "ActivateActivityType" : "DeactivateActivityType",
      "ActivityTypeDefinition", type.Id.ToString(),
            "IsActive", oldValue, isActive.ToString(), reason, ipAddress,
      DateTimeOffset.UtcNow), ct);

     await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Activity type {TypeName} {Action} by {OperatorId}",
            ((ActivityType)type.ActivityType).ToString(),
    isActive ? "activated" : "deactivated", operatorId);
    }

    public async Task ReorderActivityTypesAsync(
        IReadOnlyList<(Guid Id, int NewSortOrder)> reordering,
   string reason,
        string operatorId,
     string operatorRole,
        string ipAddress,
        CancellationToken ct = default)
    {
        var ids = reordering.Select(r => r.Id).ToList();
      var types = await _db.ActivityTypeDefinitions
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(ct);

        foreach (var (id, newOrder) in reordering)
        {
 var type = types.FirstOrDefault(t => t.Id == id);
     if (type == null) continue;

          var oldOrder = type.SortOrder;
            type.SortOrder = newOrder;
      type.UpdatedAt = DateTimeOffset.UtcNow;

 await _audit.RecordAsync(new AuditLogEntry(
       Guid.NewGuid(), operatorId, operatorRole,
             "ReorderActivityType", "ActivityTypeDefinition", type.Id.ToString(),
       "SortOrder", oldOrder.ToString(), newOrder.ToString(), reason, ipAddress,
     DateTimeOffset.UtcNow), ct);
     }

        await _db.SaveChangesAsync(ct);

     _logger.LogInformation("{Count} activity types reordered by {OperatorId}", reordering.Count, operatorId);
}
}
