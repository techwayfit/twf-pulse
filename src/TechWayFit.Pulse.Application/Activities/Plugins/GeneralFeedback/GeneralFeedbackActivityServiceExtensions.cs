using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.GeneralFeedback;

/// <summary>
/// DI extension to register the GeneralFeedback activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class GeneralFeedbackActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="GeneralFeedbackActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// </summary>
    public static IServiceCollection AddGeneralFeedbackActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, GeneralFeedbackActivityPlugin>();
        return services;
    }
}
