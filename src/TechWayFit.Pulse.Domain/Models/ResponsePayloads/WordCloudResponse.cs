namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Response payload for Word Cloud activity.
/// Contains a short text submission (1-N words).
/// </summary>
public sealed class WordCloudResponse
{
    public WordCloudResponse(string text)
  {
        if (string.IsNullOrWhiteSpace(text))
        {
throw new ArgumentException("Text is required.", nameof(text));
        }

 Text = text.Trim();
    }

    public string Text { get; }
}
