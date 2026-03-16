using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Rating;

/// <summary>
/// DI extension to register the Rating activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class RatingActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="RatingActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddRatingActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, RatingActivityPlugin>();
        return services;
    }
}
