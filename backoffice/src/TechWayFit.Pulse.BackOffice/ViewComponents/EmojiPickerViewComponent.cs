using Microsoft.AspNetCore.Mvc;

namespace TechWayFit.Pulse.BackOffice.ViewComponents;

/// <summary>
/// Renders an emoji picker widget — a preview tile + modal grid.
/// Usage: @await Component.InvokeAsync("EmojiPicker", new { inputName = "IconEmoji", currentValue = Model.IconEmoji })
/// </summary>
public sealed class EmojiPickerViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string inputName, string? currentValue = null, string? inputId = null)
    {
        return View(new EmojiPickerModel(
            InputName: inputName,
            InputId: inputId ?? inputName,
            CurrentValue: string.IsNullOrWhiteSpace(currentValue) ? string.Empty : currentValue));
    }
}

public record EmojiPickerModel(string InputName, string InputId, string CurrentValue);
