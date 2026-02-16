using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class AnnouncementBannerViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(AnnouncementBannerViewModel? model = null)
    {
        return View(model ?? new AnnouncementBannerViewModel());
    }
}
