using TechWayFit.Pulse.BackOffice.Core.Models.Commercialization;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// BackOffice operations for activity type definition management.
/// Controls which activities are premium vs free, their display metadata, etc.
/// SuperAdmin only - impacts product positioning and feature gating.
/// </summary>
public interface IBackOfficeActivityTypeService
{
  
    
    Task<ActivityTypeSearchResult> SearchActivityTypesAsync(
        ActivityTypeSearchQuery query,
        CancellationToken ct = default);
    
    Task<ActivityTypeDefinitionDetail?> GetActivityTypeDetailAsync(
   Guid id,
        CancellationToken ct = default);
    
    Task<ActivityTypeDefinitionDetail> CreateActivityTypeAsync(
        CreateActivityTypeDefinitionRequest request,
string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
    Task UpdateActivityTypeAsync(
        UpdateActivityTypeDefinitionRequest request,
        string operatorId,
   string operatorRole,
   string ipAddress,
 CancellationToken ct = default);
    
    /// <summary>Toggle premium status (zero-deployment feature gating)</summary>
    Task TogglePremiumAsync(
      TogglePremiumRequest request,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
    /// <summary>Toggle active status (hide from UI)</summary>
    Task ToggleActiveAsync(
        Guid id,
        bool isActive,
        string reason,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
    
  
    
    /// <summary>Reorder activity types (updates SortOrder)</summary>
  Task ReorderActivityTypesAsync(
        IReadOnlyList<(Guid Id, int NewSortOrder)> reordering,
        string reason,
        string operatorId,
        string operatorRole,
        string ipAddress,
        CancellationToken ct = default);
}
