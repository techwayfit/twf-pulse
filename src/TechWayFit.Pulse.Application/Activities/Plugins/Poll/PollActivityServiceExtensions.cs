using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Application.Activities.Plugins.Poll;

/// <summary>
/// DI extension to register the Poll activity plugin.
/// Must be called AFTER <c>services.AddActivityPlugins()</c>.
/// </summary>
public static class PollActivityServiceExtensions
{
    /// <summary>
    /// Registers <see cref="PollActivityPlugin"/> as an <see cref="IActivityPlugin"/> singleton.
    /// <see cref="IActivityRegistry"/> discovers it automatically via <c>IEnumerable&lt;IActivityPlugin&gt;</c>.
    /// </summary>
    public static IServiceCollection AddPollActivity(this IServiceCollection services)
    {
        services.AddSingleton<IActivityPlugin, PollActivityPlugin>();
        return services;
    }
}
