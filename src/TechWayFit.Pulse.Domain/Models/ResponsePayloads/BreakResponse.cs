using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Response payload for Break activity type.
/// Submitted when a participant signals they are ready to continue.
/// </summary>
public sealed class BreakResponse
{
    public BreakResponse()
    {
        Ready = true;
    }

    [JsonConstructor]
    public BreakResponse(bool ready = true)
    {
        Ready = ready;
    }

    /// <summary>
    /// True when the participant has signalled they are back from break.
    /// </summary>
    [JsonPropertyName("ready")]
    public bool Ready { get; set; }
}
