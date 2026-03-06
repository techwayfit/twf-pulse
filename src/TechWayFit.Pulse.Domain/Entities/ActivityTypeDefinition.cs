using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Defines metadata and access rules for an activity type.
/// System-defined, operator-managed via BackOffice.
/// Links ActivityType enum to display metadata and premium access requirements.
/// </summary>
public sealed class ActivityTypeDefinition
{
    public ActivityTypeDefinition(
Guid id,
        ActivityType activityType,
        string displayName,
        string description,
   string iconClass,
        string colorHex,
        bool requiresPremium,
   string? applicablePlanIds,
      bool isAvailableToAllPlans,
        bool isActive,
        int sortOrder,
    DateTimeOffset createdAt,
  DateTimeOffset updatedAt)
    {
 if (string.IsNullOrWhiteSpace(displayName))
   throw new ArgumentException("Display name is required.", nameof(displayName));
    if (string.IsNullOrWhiteSpace(description))
      throw new ArgumentException("Description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(iconClass))
         throw new ArgumentException("Icon class is required.", nameof(iconClass));
if (string.IsNullOrWhiteSpace(colorHex))
  throw new ArgumentException("Color hex is required.", nameof(colorHex));

        Id = id;
        ActivityType = activityType;
        DisplayName = displayName;
Description = description;
  IconClass = iconClass;
     ColorHex = colorHex;
    RequiresPremium = requiresPremium;
ApplicablePlanIds = applicablePlanIds;
        IsAvailableToAllPlans = isAvailableToAllPlans;
   IsActive = isActive;
  SortOrder = sortOrder;
      CreatedAt = createdAt;
    UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    /// <summary>
    /// Links to ActivityType enum (Poll=0, WordCloud=2, FiveWhys=6, etc.)
/// </summary>
    public ActivityType ActivityType { get; }

    /// <summary>
    /// User-facing display name (e.g., "5 Whys Analysis", "AI Summary")
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Feature description shown in activity picker
/// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// CSS icon class (e.g., "ics ics-question ic-sm" or "fas fa-robot")
    /// </summary>
public string IconClass { get; private set; }

    /// <summary>
    /// Hex color for badge/card background (e.g., "#6C5CE7")
    /// </summary>
    public string ColorHex { get; private set; }

    /// <summary>
/// Whether this activity type requires a premium plan
    /// If true, free plan users cannot use this activity type
    /// </summary>
    public bool RequiresPremium { get; private set; }

    /// <summary>
    /// Pipe-separated list of plan IDs that can access this activity.
    /// Example: "guid1|guid2|guid3"
 /// Null if IsAvailableToAllPlans = true
    /// </summary>
    public string? ApplicablePlanIds { get; private set; }

    /// <summary>
    /// If true, all plans (including free) can use this activity.
    /// If false, check ApplicablePlanIds for access control.
    /// </summary>
    public bool IsAvailableToAllPlans { get; private set; }

    /// <summary>
    /// Whether this activity type is active and visible in UI
    /// False = hidden (for unimplemented or deprecated activity types)
/// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Display order in activity picker (lower numbers first)
    /// </summary>
    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(
        string displayName,
   string description,
  string iconClass,
     string colorHex,
bool requiresPremium,
        string? applicablePlanIds,
        bool isAvailableToAllPlans,
  bool isActive,
     int sortOrder,
  DateTimeOffset updatedAt)
{
  if (string.IsNullOrWhiteSpace(displayName))
   throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(description))
      throw new ArgumentException("Description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(iconClass))
         throw new ArgumentException("Icon class is required.", nameof(iconClass));
        if (string.IsNullOrWhiteSpace(colorHex))
          throw new ArgumentException("Color hex is required.", nameof(colorHex));

        DisplayName = displayName;
        Description = description;
        IconClass = iconClass;
        ColorHex = colorHex;
        RequiresPremium = requiresPremium;
     ApplicablePlanIds = applicablePlanIds;
        IsAvailableToAllPlans = isAvailableToAllPlans;
    IsActive = isActive;
SortOrder = sortOrder;
    UpdatedAt = updatedAt;
  }
}
