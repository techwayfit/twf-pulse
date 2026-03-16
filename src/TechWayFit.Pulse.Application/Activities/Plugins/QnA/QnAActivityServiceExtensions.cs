using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.QnA;

/// <summary>
/// DI extension to register the QnA activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class QnAActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="QnAActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddQnAActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, QnAActivityPlugin>();
        return services;
    }
}
