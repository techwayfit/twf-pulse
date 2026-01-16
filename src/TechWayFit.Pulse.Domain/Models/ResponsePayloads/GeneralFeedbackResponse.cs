namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Response payload for General Feedback activity.
/// Contains long-form text with optional category and anonymity flag.
/// </summary>
public sealed class GeneralFeedbackResponse
{
  public GeneralFeedbackResponse(
        string text,
        string? category = null,
        bool isAnonymous = false)
    {
    if (string.IsNullOrWhiteSpace(text))
        {
  throw new ArgumentException("Feedback text is required.", nameof(text));
 }

     Text = text.Trim();
 Category = category?.Trim();
        IsAnonymous = isAnonymous;
        CharacterCount = text.Length;
    }

    public string Text { get; }
    public string? Category { get; }
    public bool IsAnonymous { get; }
    public int CharacterCount { get; }
}
