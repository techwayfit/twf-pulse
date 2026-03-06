using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for ActivityTypeDefinitionRecord to handle plan-based access control
/// </summary>
public static class ActivityTypeExtensions
{
    /// <summary>
    /// Parse pipe-separated plan IDs into a list of GUIDs
    /// </summary>
    public static List<Guid> GetApplicablePlanIds(this ActivityTypeDefinitionRecord activityType)
{
        if (activityType.IsAvailableToAllPlans || string.IsNullOrWhiteSpace(activityType.ApplicablePlanIds))
        {
            return new List<Guid>();
   }

        return activityType.ApplicablePlanIds
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
       .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
        .ToList();
    }

    /// <summary>
    /// Check if a specific plan can access this activity type
    /// </summary>
    public static bool CanPlanAccess(this ActivityTypeDefinitionRecord activityType, Guid userPlanId)
    {
        // Available to all plans (free activities)
  if (activityType.IsAvailableToAllPlans)
            return true;

      // Premium - check if user's plan is in the applicable plans list
        var applicablePlans = activityType.GetApplicablePlanIds();
        return applicablePlans.Contains(userPlanId);
    }

    /// <summary>
  /// Convert list of plan IDs to pipe-separated string for database storage
    /// </summary>
    public static string ToPipeSeparatedString(this IEnumerable<Guid> planIds)
    {
    return string.Join("|", planIds.Select(id => id.ToString()));
    }

    /// <summary>
 /// Get a human-readable list of applicable plan names
    /// </summary>
    public static string GetApplicablePlanNamesDisplay(
        this ActivityTypeDefinitionRecord activityType, 
        Dictionary<Guid, string> planLookup)
    {
        if (activityType.IsAvailableToAllPlans)
   return "All Plans";

        var applicablePlanIds = activityType.GetApplicablePlanIds();
 if (!applicablePlanIds.Any())
 return "No Plans";

        var planNames = applicablePlanIds
         .Where(id => planLookup.ContainsKey(id))
            .Select(id => planLookup[id])
       .ToList();

        return planNames.Any() ? string.Join(", ", planNames) : "Unknown Plans";
 }
}
