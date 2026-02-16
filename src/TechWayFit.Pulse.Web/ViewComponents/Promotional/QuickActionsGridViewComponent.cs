using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class QuickActionsGridViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(QuickActionsGridViewModel? model = null)
    {
        return View(model ?? new QuickActionsGridViewModel());
    }
}
