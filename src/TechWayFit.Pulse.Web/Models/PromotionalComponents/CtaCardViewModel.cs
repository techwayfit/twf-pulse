namespace TechWayFit.Pulse.Web.Models.PromotionalComponents;

public class CtaCardViewModel
{
    public string BadgeText { get; set; } = "Resource";
    public string Title { get; set; } = "Workshop Facilitator's Guide";
    public string Description { get; set; } = "Download our comprehensive guide with tips, templates, and best practices for running engaging virtual workshops.";
    public string PrimaryActionText { get; set; } = "Download Guide";
    public string PrimaryActionUrl { get; set; } = "#";
    public string SecondaryActionText { get; set; } = "View Sample";
    public string SecondaryActionUrl { get; set; } = "#";
    public string ImageIconClass { get; set; } = "fas fa-file-alt";
    public string ImageText { get; set; } = "Preview Image";
}
