namespace TechWayFit.Pulse.Web.Models.PromotionalComponents;

public class NotificationStripViewModel
{
    public string Message { get; set; } = "New: Presentation Mode is now live! Share your session results on the big screen.";
    public string LinkText { get; set; } = "Learn More";
    public string LinkUrl { get; set; } = "#";
    public bool ShowPulseIndicator { get; set; } = true;
}
