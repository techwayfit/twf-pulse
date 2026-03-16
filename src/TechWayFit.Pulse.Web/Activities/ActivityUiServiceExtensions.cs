using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Activities.Abstractions;

namespace TechWayFit.Pulse.Web.Activities;

/// <summary>
/// DI extensions for the Web layer activity UI infrastructure.
/// Call <see cref="AddActivityUi"/> once in <c>Program.cs</c>; then call
/// each activity's individual <c>AddXxxActivityUi()</c> extension to register
/// its <see cref="IActivityUiDescriptor"/>.
/// </summary>
public static class ActivityUiServiceExtensions
{
    /// <summary>
    /// Registers the core Web-layer activity UI infrastructure:
    /// <list type="bullet">
    ///   <item><see cref="IActivityUiRegistry"/> (singleton)</item>
    ///   <item><see cref="IActivityDefaults"/> adapter backed by <c>ActivityDefaultsOptions</c> (singleton)</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddActivityUi(this IServiceCollection services)
    {
        services.AddSingleton<IActivityUiRegistry, ActivityUiRegistry>();
        services.AddSingleton<IActivityDefaults, ActivityDefaultsAdapter>();
        return services;
    }
}
