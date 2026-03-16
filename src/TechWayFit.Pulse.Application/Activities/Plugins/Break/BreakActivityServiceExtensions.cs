using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Break;

/// <summary>
/// DI extension to register the Break activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class BreakActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="BreakActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddBreakActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, BreakActivityPlugin>();
        return services;
    }
}
