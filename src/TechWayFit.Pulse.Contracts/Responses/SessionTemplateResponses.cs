using TechWayFit.Pulse.Contracts.Models;

namespace TechWayFit.Pulse.Contracts.Responses;

public sealed class GetTemplatesResponse
{
    public List<SessionTemplateDto> Templates { get; set; } = new();
}

public sealed class GetTemplateDetailResponse
{
    public SessionTemplateDetailDto Template { get; set; } = new();
}

public sealed class CreateTemplateResponse
{
    public Guid TemplateId { get; set; }

    public string Message { get; set; } = "Template created successfully";
}

public sealed class UpdateTemplateResponse
{
    public string Message { get; set; } = "Template updated successfully";
}
