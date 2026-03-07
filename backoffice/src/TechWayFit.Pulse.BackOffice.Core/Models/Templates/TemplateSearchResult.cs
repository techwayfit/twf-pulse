namespace TechWayFit.Pulse.BackOffice.Core.Models.Templates;

public record TemplateSearchResult(
    List<TemplateSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);
