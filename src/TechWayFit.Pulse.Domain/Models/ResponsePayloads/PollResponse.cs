namespace TechWayFit.Pulse.Domain.Models.ResponsePayloads;

/// <summary>
/// Response payload for Poll activity.
/// Contains selected option IDs and optional custom option text.
/// </summary>
public sealed class PollResponse
{
    public PollResponse(List<string> selectedOptionIds, string? customOptionText = null)
    {
        if (selectedOptionIds == null || selectedOptionIds.Count == 0)
        {
            throw new ArgumentException("At least one option must be selected.", nameof(selectedOptionIds));
    }

        SelectedOptionIds = selectedOptionIds;
        CustomOptionText = customOptionText?.Trim();
    }

    public List<string> SelectedOptionIds { get; }
    public string? CustomOptionText { get; }
}
