using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for AiSummary activity type.
/// When opened, the server uses AI to generate a summary of all previously
/// completed activities and their responses. Participants read the summary only;
/// no response is required.
/// This type is excluded from AI agenda generation and AI summary context.
/// </summary>
public sealed class AiSummaryConfig
{
    public AiSummaryConfig()
    {
        CustomPromptAddition = string.Empty;
        ShowActivityBreakdown = true;
        GeneratedSummary = string.Empty;
        IsGenerating = false;
        GeneratedAt = null;
    }

    [JsonConstructor]
    public AiSummaryConfig(
        string customPromptAddition = "",
        bool showActivityBreakdown = true,
        string generatedSummary = "",
        bool isGenerating = false,
        DateTimeOffset? generatedAt = null)
    {
        CustomPromptAddition = customPromptAddition;
        ShowActivityBreakdown = showActivityBreakdown;
        GeneratedSummary = generatedSummary;
        IsGenerating = isGenerating;
        GeneratedAt = generatedAt;
    }

    /// <summary>
    /// Optional extra instruction appended to the AI prompt for this specific summary.
    /// </summary>
    [JsonPropertyName("customPromptAddition")]
    public string CustomPromptAddition { get; set; }

    /// <summary>
    /// Whether to show a per-activity breakdown alongside the overall summary.
    /// </summary>
    [JsonPropertyName("showActivityBreakdown")]
    public bool ShowActivityBreakdown { get; set; }

    /// <summary>
    /// The AI-generated summary text. Populated by the server when the activity is opened.
    /// Empty string means not yet generated.
    /// </summary>
    [JsonPropertyName("generatedSummary")]
    public string GeneratedSummary { get; set; }

    /// <summary>
    /// True while the AI is still generating the summary.
    /// </summary>
    [JsonPropertyName("isGenerating")]
    public bool IsGenerating { get; set; }

    /// <summary>
    /// When the summary was generated (UTC). Null if not yet generated.
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public DateTimeOffset? GeneratedAt { get; set; }
}
