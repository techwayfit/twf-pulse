namespace TechWayFit.Pulse.Web.Models.PromotionalComponents;

public class StatsBannerViewModel
{
    public IReadOnlyList<StatsBannerItemViewModel> Items { get; set; } = Array.Empty<StatsBannerItemViewModel>();
}

public class StatsBannerItemViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string SubLabel { get; set; } = string.Empty;
}
