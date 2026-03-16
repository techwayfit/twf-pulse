using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Web.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Break;

/// <summary>
/// DI extension to register the Break activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class BreakWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="BreakActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// </summary>
    public static IServiceCollection AddBreakActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, BreakActivityUiDescriptor>();
        return services;
    }
}
