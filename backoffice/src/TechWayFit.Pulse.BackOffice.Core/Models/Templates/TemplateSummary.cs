using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Core.Models.Templates;

public record TemplateSummary(
    Guid Id,
    string Name,
    string Description,
    TemplateCategory Category,
    string IconEmoji,
    bool IsSystemTemplate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
