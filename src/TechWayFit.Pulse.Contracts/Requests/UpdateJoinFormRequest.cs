using TechWayFit.Pulse.Contracts.Models;

namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class UpdateJoinFormRequest
{
    public JoinFormSchemaDto JoinFormSchema { get; set; } = new();
}
