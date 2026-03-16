using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Activities.Abstractions;

/// <summary>
/// Central registry of all registered activity plugins.
/// Replaces every <c>if/else ActivityType == ...</c> dispatch chain.
/// Plugins are registered via DI (<c>IEnumerable&lt;IActivityPlugin&gt;</c>)
/// so adding a new activity never requires changing this interface or its implementation.
/// </summary>
public interface IActivityRegistry
{
    /// <summary>
    /// Returns the plugin for the given <paramref name="type"/>.
    /// Throws <see cref="InvalidOperationException"/> if no plugin is registered.
    /// </summary>
    IActivityPlugin GetPlugin(ActivityType type);

    /// <summary>Returns all registered activity plugins in registration order.</summary>
    IReadOnlyList<IActivityPlugin> GetAll();

    /// <summary>Returns plugins whose <see cref="IActivityPlugin.CanBeAiGenerated"/> is true.</summary>
    IReadOnlyList<IActivityPlugin> GetAiGeneratable();

    /// <summary>Returns plugins whose <see cref="IActivityPlugin.IncludeInAiSummary"/> is true.</summary>
    IReadOnlyList<IActivityPlugin> GetIncludedInAiSummary();

    /// <summary>Returns true if a plugin is registered for the given <paramref name="type"/>.</summary>
    bool IsRegistered(ActivityType type);
}
