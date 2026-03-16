using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Web.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.AiSummary;

/// <summary>
/// DI extension to register the AiSummary activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class AiSummaryWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="AiSummaryActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// </summary>
    public static IServiceCollection AddAiSummaryActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, AiSummaryActivityUiDescriptor>();
        return services;
    }
}
