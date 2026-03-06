using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for activity type definitions (read-only for main app)
/// </summary>
public sealed class ActivityTypeDefinitionRepository : IActivityTypeDefinitionRepository
{
    private readonly PulseMariaDbContext _context;

    public ActivityTypeDefinitionRepository(PulseMariaDbContext context)
{
        _context = context;
    }

    public async Task<ActivityTypeDefinition?> GetByActivityTypeAsync(
      ActivityType activityType,
        CancellationToken cancellationToken = default)
    {
        var record = await _context.ActivityTypeDefinitions
            .AsNoTracking()
  .FirstOrDefaultAsync(a => a.ActivityType == (int)activityType, cancellationToken);

        return record != null ? MapToDomain(record) : null;
  }

    public async Task<IReadOnlyList<ActivityTypeDefinition>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await _context.ActivityTypeDefinitions
            .AsNoTracking()
            .Where(a => a.IsActive)
    .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.ActivityType)
          .ToListAsync(cancellationToken);

     return records.Select(MapToDomain).ToList();
    }

    public async Task<ActivityTypeDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var record = await _context.ActivityTypeDefinitions
            .AsNoTracking()
   .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

  return record != null ? MapToDomain(record) : null;
    }

    private static ActivityTypeDefinition MapToDomain(ActivityTypeDefinitionRecord record)
    {
        return new ActivityTypeDefinition(
            id: record.Id,
     activityType: (ActivityType)record.ActivityType,
            displayName: record.DisplayName,
   description: record.Description,
       iconClass: record.IconClass,
            colorHex: record.ColorHex,
 requiresPremium: record.RequiresPremium,
       applicablePlanIds: record.ApplicablePlanIds,
  isAvailableToAllPlans: record.IsAvailableToAllPlans,
          isActive: record.IsActive,
   sortOrder: record.SortOrder,
         createdAt: record.CreatedAt,
        updatedAt: record.UpdatedAt);
    }
}
