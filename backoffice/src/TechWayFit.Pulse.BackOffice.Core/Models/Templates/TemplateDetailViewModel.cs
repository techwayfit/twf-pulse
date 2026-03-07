using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Core.Models.Templates;

public record TemplateDetailViewModel(
    Guid Id,
    string Name,
    string Description,
    TemplateCategory Category,
    string IconEmoji,
    string ConfigJson,
    bool IsSystemTemplate,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
