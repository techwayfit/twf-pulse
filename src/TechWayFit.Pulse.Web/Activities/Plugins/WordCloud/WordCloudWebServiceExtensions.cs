using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Web.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.WordCloud;

/// <summary>
/// DI extension to register the WordCloud activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class WordCloudWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="WordCloudActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// </summary>
    public static IServiceCollection AddWordCloudActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, WordCloudActivityUiDescriptor>();
        return services;
    }
}
