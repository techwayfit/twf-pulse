using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Models.PromotionalComponents;

namespace TechWayFit.Pulse.Web.ViewComponents.Promotional;

public class TestimonialCardViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(TestimonialCardViewModel? model = null)
    {
        return View(model ?? new TestimonialCardViewModel());
    }
}
