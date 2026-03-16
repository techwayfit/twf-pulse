using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Web.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.QnA;

/// <summary>
/// DI extension to register the QnA activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class QnAWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="QnAActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// </summary>
    public static IServiceCollection AddQnAActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, QnAActivityUiDescriptor>();
        return services;
    }
}
