using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Sqlite;
using TechWayFit.Pulse.BackOffice.Core.Persistence.SqlServer;
using TechWayFit.Pulse.BackOffice.Core.Services;

namespace TechWayFit.Pulse.BackOffice.Core;

public static class BackOfficeCoreServiceExtensions
{
    /// <summary>
    /// Registers all BackOffice.Core services including the shared DbContext.
    /// Selects the database provider based on <c>Pulse:DatabaseProvider</c> config
    /// ("SqlServer", "MariaDB", "MySQL", or "Sqlite" — defaults to "Sqlite").
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
        else if (provider.Equals("MariaDB", StringComparison.OrdinalIgnoreCase) ||
                 provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<BackOfficeMariaDbContext>(options =>
                options.UseMySQL(connectionString));

            services.AddScoped<BackOfficeDbContext>(sp => sp.GetRequiredService<BackOfficeMariaDbContext>());
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
        services.AddScoped<IBackOfficeTemplateService, BackOfficeTemplateService>();
        services.AddScoped<IBackOfficeAuthService, BackOfficeAuthService>();

        // Cache management — calls main app's internal API via named HttpClient.
        // Requires MainApp:BaseUrl and MainApp:BackOfficeApiToken in appsettings.
        services.AddHttpClient(BackOfficeCacheService.HttpClientName, (sp, client) =>
        {
            var cfg     = sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["MainApp:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            }
        });
        services.AddScoped<IBackOfficeCacheService, BackOfficeCacheService>();

        return services;
    }
}
