using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Application.Activities.Registry;

/// <summary>
/// Singleton registry populated from all <see cref="IActivityPlugin"/> registrations.
/// All dispatch chains previously expressed as <c>if/else ActivityType == ...</c>
/// now resolve through this registry via O(1) dictionary lookup.
/// </summary>
public sealed class ActivityRegistry : IActivityRegistry
{
    private readonly IReadOnlyDictionary<ActivityType, IActivityPlugin> _plugins;

    public ActivityRegistry(IEnumerable<IActivityPlugin> plugins)
    {
        _plugins = plugins.ToDictionary(p => p.ActivityType);
    }

    /// <inheritdoc />
    public IActivityPlugin GetPlugin(ActivityType type)
    {
        return _plugins.TryGetValue(type, out var plugin)
            ? plugin
            : throw new InvalidOperationException(
                $"No activity plugin is registered for ActivityType '{type}'. " +
                $"Add services.AddSingleton<IActivityPlugin, YourPlugin>() in the activity's registration.");
    }

    /// <inheritdoc />
    public IReadOnlyList<IActivityPlugin> GetAll()
        => _plugins.Values.ToList();

    /// <inheritdoc />
    public IReadOnlyList<IActivityPlugin> GetAiGeneratable()
        => _plugins.Values.Where(p => p.CanBeAiGenerated).ToList();

    /// <inheritdoc />
    public IReadOnlyList<IActivityPlugin> GetIncludedInAiSummary()
        => _plugins.Values.Where(p => p.IncludeInAiSummary).ToList();

    /// <inheritdoc />
    public bool IsRegistered(ActivityType type)
        => _plugins.ContainsKey(type);
}
