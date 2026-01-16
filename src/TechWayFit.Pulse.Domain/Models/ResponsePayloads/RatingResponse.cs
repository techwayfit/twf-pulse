namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Response payload for Rating activity.
/// Contains numeric rating and optional comment.
/// </summary>
public sealed class RatingResponse
{
    public RatingResponse(int rating, int scale, string? comment = null)
    {
if (rating < 1 || rating > scale)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), $"Rating must be between 1 and {scale}.");
        }

        Rating = rating;
        Scale = scale;
        Comment = comment?.Trim();
    }

    public int Rating { get; }
    public int Scale { get; }
    public string? Comment { get; }
}
