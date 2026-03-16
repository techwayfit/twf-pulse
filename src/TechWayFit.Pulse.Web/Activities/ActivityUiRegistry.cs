using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Web.Activities;

/// <summary>
/// Singleton registry populated from all <see cref="IActivityUiDescriptor"/> registrations.
/// </summary>
public sealed class ActivityUiRegistry : IActivityUiRegistry
{
    private readonly IReadOnlyDictionary<ActivityType, IActivityUiDescriptor> _descriptors;

    public ActivityUiRegistry(IEnumerable<IActivityUiDescriptor> descriptors)
    {
        _descriptors = descriptors.ToDictionary(d => d.ActivityType);
    }

    /// <inheritdoc />
    public IActivityUiDescriptor GetDescriptor(ActivityType type)
        => _descriptors.TryGetValue(type, out var d) ? d : NullActivityUiDescriptor.Instance;

    /// <inheritdoc />
    public IReadOnlyList<IActivityUiDescriptor> GetAll()
        => _descriptors.Values.ToList();
}

/// <summary>
/// Returned when no UI descriptor is registered for an activity type.
/// All component type properties return null, causing the Blazor render to show
/// a "coming soon" fallback — same as the current else-branch behaviour.
/// </summary>
internal sealed class NullActivityUiDescriptor : IActivityUiDescriptor
{
    public static readonly NullActivityUiDescriptor Instance = new();

    public ActivityType ActivityType => default;
    public Type? ParticipantComponentType   => null;
    public Type? DashboardComponentType     => null;
    public Type? PresentationComponentType  => null;
    public Type? EditConfigComponentType    => null;
    public Type? CreateModalComponentType   => null;
}
