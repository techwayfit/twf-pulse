using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Quadrant;

/// <summary>
/// DI extension to register the Quadrant activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class QuadrantActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="QuadrantActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddQuadrantActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, QuadrantActivityPlugin>();
        return services;
    }
}
