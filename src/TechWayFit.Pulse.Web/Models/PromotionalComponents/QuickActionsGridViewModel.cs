namespace TechWayFit.Pulse.Web.Models.PromotionalComponents;

public class QuickActionsGridViewModel
{
    public IReadOnlyList<QuickActionItemViewModel> Items { get; set; } = Array.Empty<QuickActionItemViewModel>();
}

public class QuickActionItemViewModel
{
    public string Icon { get; set; } = "pin.svg";
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = "#";
}
