using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Web.Activities;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Rating;

/// <summary>
/// DI extension to register the Rating activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class RatingWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="RatingActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// </summary>
    public static IServiceCollection AddRatingActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, RatingActivityUiDescriptor>();
        return services;
    }
}
