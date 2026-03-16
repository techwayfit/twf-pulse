using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Web.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Quadrant;

/// <summary>
/// DI extension to register the Quadrant activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class QuadrantWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="QuadrantActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// </summary>
    public static IServiceCollection AddQuadrantActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, QuadrantActivityUiDescriptor>();
        return services;
    }
}
