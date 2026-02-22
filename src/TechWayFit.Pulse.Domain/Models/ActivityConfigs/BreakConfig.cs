using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Break activity type.
/// Allows facilitator to schedule a timed break with a custom message.
/// Participants can indicate they are back via a "Ready" response.
/// This type is excluded from AI agenda generation and AI summary context.
/// </summary>
public sealed class BreakConfig
{
    public BreakConfig()
    {
        Message = "Take a short break. We'll resume shortly!";
        DurationMinutes = 15;
        ShowCountdown = true;
        AllowReadySignal = true;
    }

    [JsonConstructor]
    public BreakConfig(
        string message = "Take a short break. We'll resume shortly!",
        int durationMinutes = 15,
        bool showCountdown = true,
        bool allowReadySignal = true)
    {
        Message = message;
        DurationMinutes = durationMinutes;
        ShowCountdown = showCountdown;
        AllowReadySignal = allowReadySignal;
    }

    /// <summary>
    /// Message displayed to participants during the break.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// Duration of the break in minutes. Drives the countdown timer.
    /// </summary>
    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Whether to show a live countdown timer to participants.
    /// </summary>
    [JsonPropertyName("showCountdown")]
    public bool ShowCountdown { get; set; }

    /// <summary>
    /// Whether participants can signal they are back from the break.
    /// </summary>
    [JsonPropertyName("allowReadySignal")]
    public bool AllowReadySignal { get; set; }
}
