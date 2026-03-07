using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Core.Models.Templates;

public record TemplateSearchQuery(
    string? NameContains,
    TemplateCategory? Category,
    bool? IsSystem,
    int Page,
    int PageSize = 30);
