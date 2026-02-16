using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class FeatureHighlightsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(FeatureHighlightsViewModel? model = null)
    {
        return View(model ?? new FeatureHighlightsViewModel());
    }
}
