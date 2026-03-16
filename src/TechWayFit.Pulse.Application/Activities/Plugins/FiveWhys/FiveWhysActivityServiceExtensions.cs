using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.FiveWhys;

/// <summary>
/// DI extension to register the FiveWhys activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class FiveWhysActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="FiveWhysActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddFiveWhysActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, FiveWhysActivityPlugin>();
        return services;
    }
}
