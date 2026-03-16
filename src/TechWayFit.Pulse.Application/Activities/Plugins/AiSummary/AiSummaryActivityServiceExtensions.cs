using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.AiSummary;

/// <summary>
/// DI extension to register the AiSummary activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class AiSummaryActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="AiSummaryActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddAiSummaryActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, AiSummaryActivityPlugin>();
        return services;
    }
}
