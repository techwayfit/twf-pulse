using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class CtaCardViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(CtaCardViewModel? model = null)
    {
        return View(model ?? new CtaCardViewModel());
    }
}
