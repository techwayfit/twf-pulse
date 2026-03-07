using TechWayFit.Pulse.BackOffice.Core.Models.Templates;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// BackOffice CRUD operations for session templates.
/// All mutations write audit records before committing.
/// </summary>
public interface IBackOfficeTemplateService
{
    Task<TemplateSearchResult> SearchAsync(TemplateSearchQuery query, CancellationToken ct = default);

    Task<TemplateDetailViewModel?> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>Create a new operator-managed template. Returns the new template id.</summary>
    Task<Guid> CreateAsync(SaveTemplateRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Update name, description, category, icon, and configJson of any template.</summary>
    Task UpdateAsync(Guid id, SaveTemplateRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Permanently delete a template (SuperAdmin only).</summary>
    Task DeleteAsync(Guid id, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);
}
