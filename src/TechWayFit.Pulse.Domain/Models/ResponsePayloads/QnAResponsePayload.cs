namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Response payload for Q&amp;A activity.
/// <para>
/// Two record types share this payload, discriminated by <see cref="Type"/>:
/// <list type="bullet">
///   <item><description><c>question</c> — a participant-submitted question.</description></item>
///   <item><description><c>vote</c> — an upvote on an existing question, identified by <see cref="QuestionResponseId"/>.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class QnAResponsePayload
{
    /// <summary>"question" or "vote".</summary>
    public string Type { get; set; } = "question";

    /// <summary>Question text (set when Type == "question").</summary>
    public string? Text { get; set; }

    /// <summary>Whether the question was submitted anonymously (Type == "question" only).</summary>
    public bool IsAnonymous { get; set; }

    /// <summary>ID of the question response entity being upvoted (set when Type == "vote").</summary>
    public string? QuestionResponseId { get; set; }
}
