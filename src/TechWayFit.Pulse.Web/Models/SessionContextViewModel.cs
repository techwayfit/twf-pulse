namespace TechWayFit.Pulse.Web.Models;

public class SessionContextViewModel
{
    public string SessionTitle { get; set; } = "Ops Pain Points Workshop — Group 1";
    public string SessionGoal { get; set; } = "Identify bottlenecks and quantify impact so we can prioritize fixes.";
    public string WorkshopType { get; set; } = "Ops / Process Improvement";
    public string PrimaryActivity { get; set; } = "Quadrant (2x2 Matrix)";

    
    // AI Context fields
    public string CurrentProcess { get; set; } = "";
    public string PainPoints { get; set; } = "";
    public string TechnicalContext { get; set; } = "";
    public string TeamBackground { get; set; } = "";
    public string AiGoals { get; set; } = "";
}
