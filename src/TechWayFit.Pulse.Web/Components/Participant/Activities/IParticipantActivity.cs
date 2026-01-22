namespace TechWayFit.Pulse.Web.Components.Participant.Activities;

/// <summary>
/// Callback for activity submission events
/// </summary>
public delegate Task ActivitySubmittedCallback(string payload);

/// <summary>
/// Parameters passed to participant activity components
/// </summary>
public class ParticipantActivityParameters
{
    public required string SessionCode { get; init; }
    public required Guid ParticipantId { get; init; }
    public required Guid ActivityId { get; init; }
    public required string Config { get; init; }
    public bool HasSubmitted { get; set; }
    public bool IsSubmitting { get; set; }
    public string SubmitMessage { get; set; } = "";
    public bool SubmitSuccess { get; set; }
    
    public required ActivitySubmittedCallback OnSubmit { get; init; }
}
