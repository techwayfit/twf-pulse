using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Sqlite;
using TechWayFit.Pulse.BackOffice.Core.Persistence.SqlServer;
using TechWayFit.Pulse.BackOffice.Core.Services;

namespace TechWayFit.Pulse.BackOffice.Core;

public static class BackOfficeCoreServiceExtensions
{
    /// <summary>
    /// Registers all BackOffice.Core services including the shared DbContext.
    /// Selects the database provider based on <c>Pulse:DatabaseProvider</c> config
    /// ("SqlServer" or "Sqlite" — defaults to "Sqlite").
    /// Call from <c>Program.cs</c> in TechWayFit.Pulse.BackOffice.
    /// </summary>
    public static IServiceCollection AddBackOfficeCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PulseDb")
            ?? throw new InvalidOperationException("ConnectionStrings:PulseDb is required.");

        var provider = configuration.GetValue<string>("Pulse:DatabaseProvider") ?? "Sqlite";

        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<BackOfficeSqlServerDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                    sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

            // Register the abstract base so services can inject BackOfficeDbContext
            services.AddScoped<BackOfficeDbContext>(sp => sp.GetRequiredService<BackOfficeSqlServerDbContext>());
        }
        else
        {
            services.AddDbContext<BackOfficeSqliteDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddScoped<BackOfficeDbContext>(sp => sp.GetRequiredService<BackOfficeSqliteDbContext>());
        }

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IBackOfficeUserService, BackOfficeUserService>();
        services.AddScoped<IBackOfficeSessionService, BackOfficeSessionService>();
        services.AddScoped<IBackOfficeAuthService, BackOfficeAuthService>();

        return services;
    }
}
