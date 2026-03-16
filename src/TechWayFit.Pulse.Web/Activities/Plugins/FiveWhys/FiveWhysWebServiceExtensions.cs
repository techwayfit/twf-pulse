using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Web.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.FiveWhys;

/// <summary>
/// DI extension to register the FiveWhys activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class FiveWhysWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="FiveWhysActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// </summary>
    public static IServiceCollection AddFiveWhysActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, FiveWhysActivityUiDescriptor>();
        return services;
    }
}
