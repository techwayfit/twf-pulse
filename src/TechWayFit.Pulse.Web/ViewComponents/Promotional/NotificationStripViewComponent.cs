using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class NotificationStripViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(NotificationStripViewModel? model = null)
    {
        return View(model ?? new NotificationStripViewModel());
    }
}
