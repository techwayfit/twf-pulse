namespace TechWayFit.Pulse.Contracts.Models;

public sealed class JoinFormSchemaDto
{
    public int MaxFields { get; set; }

    public List<JoinFormFieldDto> Fields { get; set; } = new();
}
