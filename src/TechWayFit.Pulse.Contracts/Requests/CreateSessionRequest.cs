using TechWayFit.Pulse.Contracts.Models;

namespace TechWayFit.Pulse.Contracts.Requests;

public sealed class CreateSessionRequest
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public string? Context { get; set; }

    public SessionSettingsDto Settings { get; set; } = new();

    public JoinFormSchemaDto JoinFormSchema { get; set; } = new();
}
