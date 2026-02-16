namespace TechWayFit.Pulse.Web.Models.PromotionalComponents;

public class FeatureHighlightsViewModel
{
    public IReadOnlyList<FeatureHighlightItemViewModel> Items { get; set; } = Array.Empty<FeatureHighlightItemViewModel>();
}

public class FeatureHighlightItemViewModel
{
    public string Icon { get; set; } = "pin.svg";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LinkText { get; set; } = "Learn More";
    public string LinkUrl { get; set; } = "#";
}
