using Microsoft.Extensions.DependencyInjection;

namespace TechWayFit.Pulse.Web.Activities.Plugins.Poll;

/// <summary>
/// DI extension to register the Poll activity's UI descriptor.
/// Must be called AFTER <c>services.AddActivityUi()</c>.
/// </summary>
public static class PollWebServiceExtensions
{
    /// <summary>
    /// Registers <see cref="PollActivityUiDescriptor"/> as an <see cref="IActivityUiDescriptor"/> singleton.
    /// <see cref="IActivityUiRegistry"/> discovers it automatically via <c>IEnumerable&lt;IActivityUiDescriptor&gt;</c>.
    /// </summary>
    public static IServiceCollection AddPollActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiDescriptor, PollActivityUiDescriptor>();
        return services;
    }
}
