using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class StatsBannerViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(StatsBannerViewModel? model = null)
    {
        return View(model ?? new StatsBannerViewModel());
    }
}
