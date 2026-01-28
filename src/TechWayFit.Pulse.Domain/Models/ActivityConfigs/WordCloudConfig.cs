using System.Text.Json.Serialization;

namespace TechWayFit.Pulse.Domain.Models.ActivityConfigs;

/// <summary>
/// Configuration for Word Cloud activity type.
/// Supports short text submissions with word frequency analysis.
/// </summary>
public sealed class WordCloudConfig
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public WordCloudConfig()
    {
        MaxWords = 3;
        MinWordLength = 3;
        MaxWordLength = 50;
        Placeholder = "Enter a word or short phrase";
        MaxSubmissionsPerParticipant = 1;
        StopWords = GetDefaultStopWords();
    }

    /// <summary>
    /// Parameterized constructor for programmatic creation with validation
    /// </summary>
    [JsonConstructor]
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

    public int MaxWords { get; set; } = 3;
    public int MinWordLength { get; set; } = 3;
    public int MaxWordLength { get; set; } = 50;
    public string Placeholder { get; set; } = "Enter a word or short phrase";
    public bool AllowMultipleSubmissions { get; set; }
    public int MaxSubmissionsPerParticipant { get; set; } = 1;
    public List<string> StopWords { get; set; } = new();
    public bool CaseSensitive { get; set; }

    private static List<string> GetDefaultStopWords()
    {
        return new List<string>
        {
            "the", "and", "is", "a", "an", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "it", "be", "this", "that"
        };
    }
}
