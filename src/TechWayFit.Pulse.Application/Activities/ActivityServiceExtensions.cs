using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;
using TechWayFit.Pulse.Application.Activities.Registry;

namespace TechWayFit.Pulse.Application.Activities;

/// <summary>
/// DI registration for the activity plugin system.
/// Call this once from the Web layer's Program.cs (or AddPulseApplicationServices extension).
/// Activity plugins themselves are registered separately via AddXxxActivity() extension methods.
/// </summary>
public static class ActivityServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IActivityRegistry"/>, <see cref="IActivityDashboardService"/>,
    /// and supporting infrastructure.
    /// Must be called AFTER all <see cref="IActivityPlugin"/> implementations are registered,
    /// so the registry can collect them via <c>IEnumerable&lt;IActivityPlugin&gt;</c>.
    /// </summary>
    public static IServiceCollection AddActivityPluginRegistry(this IServiceCollection services)
    {
        // Singleton registry — collects all IActivityPlugin registrations
        services.AddSingleton<IActivityRegistry, ActivityRegistry>();

        // Scoped data context — thin adapter over repositories
        services.AddScoped<IActivityDataContext, ActivityDataContext>();

        // Scoped unified dashboard service — replaces N individual dashboard services
        services.AddScoped<IActivityDashboardService, ActivityDashboardService>();

        return services;
    }
}
