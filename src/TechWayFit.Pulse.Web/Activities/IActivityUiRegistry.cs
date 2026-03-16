using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Web.Activities;

/// <summary>
/// Looks up <see cref="IActivityUiDescriptor"/> registrations by activity type.
/// Injected into Blazor components that previously used <c>if/else</c> dispatch chains
/// to select a component type.
/// </summary>
public interface IActivityUiRegistry
{
    /// <summary>
    /// Returns the UI descriptor for the given <paramref name="type"/>.
    /// Returns a <see cref="NullActivityUiDescriptor"/> when no descriptor is registered,
    /// so callers never receive null and never need a null check before using 
    /// <see cref="IActivityUiDescriptor.DashboardComponentType"/> etc.
    /// </summary>
    IActivityUiDescriptor GetDescriptor(ActivityType type);

    /// <summary>Returns all registered UI descriptors.</summary>
    IReadOnlyList<IActivityUiDescriptor> GetAll();
}
