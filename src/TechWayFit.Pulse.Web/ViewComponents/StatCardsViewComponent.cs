using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models;

namespace TechWayFit.Pulse.Web.ViewComponents;

public class StatCardsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(List<StatCardData> cards)
    {
        return View(cards);
    }
}
