using System.Text.Json;

namespace TechWayFit.Pulse.Domain.ValueObjects;

/// <summary>
/// Feature flags for a subscription plan, parsed from FeaturesJson column.
/// Extensible — new features can be added to JSON without schema changes.
/// </summary>
public sealed record PlanFeatures(
    bool AiAssist,
    bool FiveWhys,
    bool AiSummary)
{
    /// <summary>
    /// Parse feature flags from JSON string stored in database
    /// </summary>
    /// <param name="json">JSON string like {"aiAssist": true, "fiveWhys": true, "aiSummary": false}</param>
 /// <returns>PlanFeatures with parsed boolean flags</returns>
    public static PlanFeatures FromJson(string json)
    {
if (string.IsNullOrWhiteSpace(json))
        {
    // Default: no features enabled
         return new PlanFeatures(false, false, false);
        }

   try
        {
      using var doc = JsonDocument.Parse(json);
   var root = doc.RootElement;

      return new PlanFeatures(
   root.TryGetProperty("aiAssist", out var ai) && ai.GetBoolean(),
     root.TryGetProperty("fiveWhys", out var fw) && fw.GetBoolean(),
         root.TryGetProperty("aiSummary", out var summary) && summary.GetBoolean());
  }
        catch (JsonException)
        {
          // Invalid JSON — default to no features
   return new PlanFeatures(false, false, false);
        }
  }

    /// <summary>
    /// Serialize to JSON string for database storage
    /// </summary>
  public string ToJson()
    {
 return JsonSerializer.Serialize(new
    {
          aiAssist = AiAssist,
      fiveWhys = FiveWhys,
          aiSummary = AiSummary
     });
}

    /// <summary>
    /// Check if a specific feature is enabled (case-insensitive)
 /// </summary>
    public bool HasFeature(string featureName)
    {
        return featureName.ToLowerInvariant() switch
 {
      "aiassist" => AiAssist,
      "fivewhys" => FiveWhys,
      "aisummary" => AiSummary,
    _ => false
   };
    }
}
