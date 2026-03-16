namespace TechWayFit.Pulse.Application.Activities.Abstractions;

/// <summary>
/// Immutable display metadata for an activity type.
/// Used for rendering badges, icons, and labels in the UI without
/// activity-specific branching.
/// </summary>
/// <param name="DisplayName">Human-readable name shown in UI (e.g. "Poll").</param>
/// <param name="FaIconClass">Font Awesome CSS class (e.g. "fa-solid fa-chart-bar").</param>
/// <param name="BadgeColorHex">Background hex for the activity type badge (e.g. "#0d6efd").</param>
/// <param name="BadgeTextColorHex">Text hex for the activity type badge (e.g. "#fff").</param>
/// <param name="AcceptsResponses">
/// True when participants actively submit a response payload (Poll, WordCloud, etc.).
/// False for passive activities like Break and AiSummary.
/// </param>
/// <param name="ConfigType">CLR type of the activity's config model (e.g. typeof(PollConfig)).</param>
/// <param name="ResponsePayloadType">
/// CLR type of the participant response payload (e.g. typeof(PollResponse)).
/// Null when AcceptsResponses is false.
/// </param>
public sealed record ActivityPluginMetadata(
    string DisplayName,
    string FaIconClass,
    string BadgeColorHex,
    string BadgeTextColorHex,
    bool AcceptsResponses,
    Type ConfigType,
    Type? ResponsePayloadType);
