using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using TechWayFit.Pulse.BackOffice.Core.Services;

namespace TechWayFit.Pulse.BackOffice.Core;

public static class BackOfficeCoreServiceExtensions
{
    /// <summary>
    /// Registers all BackOffice.Core services including the shared DbContext.
    /// Call from <c>Program.cs</c> in TechWayFit.Pulse.BackOffice.
    /// </summary>
    public static IServiceCollection AddBackOfficeCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PulseDb")
            ?? throw new InvalidOperationException("ConnectionStrings:PulseDb is required.");

        services.AddDbContext<BackOfficeDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IBackOfficeUserService, BackOfficeUserService>();
        services.AddScoped<IBackOfficeSessionService, BackOfficeSessionService>();
        services.AddScoped<IBackOfficeAuthService, BackOfficeAuthService>();

        return services;
    }
}
