using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class HeroBannerViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(HeroBannerViewModel? model = null)
    {
        return View(model ?? new HeroBannerViewModel());
    }
}
