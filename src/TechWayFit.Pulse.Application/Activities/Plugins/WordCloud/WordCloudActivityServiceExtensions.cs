using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.WordCloud;

/// <summary>
/// DI extension to register the WordCloud activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class WordCloudActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="WordCloudActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddWordCloudActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, WordCloudActivityPlugin>();
        return services;
    }
}
