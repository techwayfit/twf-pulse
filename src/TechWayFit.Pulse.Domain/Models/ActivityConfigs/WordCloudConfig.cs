namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Word Cloud activity type.
/// Supports short text submissions with word frequency analysis.
/// </summary>
public sealed class WordCloudConfig
{
  public WordCloudConfig(
      int maxWords = 3,
int minWordLength = 3,
      int maxWordLength = 50,
      string? placeholder = null,
      bool allowMultipleSubmissions = false,
  int maxSubmissionsPerParticipant = 1,
List<string>? stopWords = null,
 bool caseSensitive = false)
  {
    if (maxWords < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(maxWords), "Max words must be at least 1.");
    }

    if (minWordLength < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(minWordLength), "Min word length must be at least 1.");
    }

    if (maxWordLength < minWordLength)
    {
      throw new ArgumentException("Max word length cannot be less than min word length.");
    }

    MaxWords = maxWords;
    MinWordLength = minWordLength;
    MaxWordLength = maxWordLength;
    Placeholder = placeholder ?? "Enter a word or short phrase";
    AllowMultipleSubmissions = allowMultipleSubmissions;
    MaxSubmissionsPerParticipant = maxSubmissionsPerParticipant;
    StopWords = stopWords ?? GetDefaultStopWords();
    CaseSensitive = caseSensitive;
  }

  public int MaxWords { get; }
  public int MinWordLength { get; }
  public int MaxWordLength { get; }
  public string Placeholder { get; }
  public bool AllowMultipleSubmissions { get; }
  public int MaxSubmissionsPerParticipant { get; }
  public List<string> StopWords { get; }
  public bool CaseSensitive { get; }

  private static List<string> GetDefaultStopWords()
  {
    return new List<string>
 {
     "the", "and", "is", "a", "an", "in", "on", "at", "to", "for",
     "of", "with", "by", "from", "as", "it", "be", "this", "that"
   };
  }
}
