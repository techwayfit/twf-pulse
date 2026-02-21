using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using SqlServerRepos = TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;
using SqliteRepos = TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;
using TechWayFit.Pulse.Infrastructure.Persistence.Sqlite;

namespace TechWayFit.Pulse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring database services.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Adds TechWayFit Pulse database services based on configuration.
    /// </summary>
    public static IServiceCollection AddPulseDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useInMemory = configuration.GetValue<bool>("Pulse:UseInMemory");
        var databaseProvider = configuration.GetValue<string>("Pulse:DatabaseProvider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("PulseDb");

        if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddPulseInMemoryDatabase();
        }
        else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddPulseSqlServerDatabase(connectionString);
        }
        else
        {
            services.AddPulseSqliteDatabase(connectionString);
        }

        return services;
    }

    /// <summary>
    /// Adds InMemory database for development/testing.
    /// </summary>
    private static IServiceCollection AddPulseInMemoryDatabase(this IServiceCollection services)
    {
        services.AddDbContext<PulseSqlLiteDbContext>(options =>
        {
            options.UseInMemoryDatabase("Pulse");
        });

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseSqlLiteDbContext>());

        // Register standard repositories for InMemory
        services.AddStandardRepositories();

        return services;
    }

    /// <summary>
    /// Adds SQLite database with appropriate configuration.
    /// </summary>
    private static IServiceCollection AddPulseSqliteDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<PulseSqlLiteDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly("TechWayFit.Pulse.Web");
            });
        });

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseSqlLiteDbContext>());

        // Register standard repositories for SQLite
        services.AddStandardRepositories();

        return services;
    }

    /// <summary>
    /// Adds SQL Server database with appropriate configuration and retry logic.
    /// NOTE: EF Migrations are DISABLED for SQL Server.
    /// All schema changes must be applied via SQL scripts in Infrastructure/Scripts/
    /// </summary>
    private static IServiceCollection AddPulseSqlServerDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<PulseSqlServerDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                // Enable retry on failure for transient errors
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                // Set command timeout
                sqlServerOptions.CommandTimeout(30);

                // Disable migrations - use manual SQL scripts only
                sqlServerOptions.MigrationsHistoryTable("__MigrationHistory", "pulse");
            });
        });

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseSqlServerDbContext>());

        // Register SQL Server-optimized repositories
        services.AddSqlServerRepositories();

        return services;
    }

    /// <summary>
    /// Registers SQL Server-optimized repository implementations.
    /// These repositories use server-side sorting and pagination for better performance.
    /// </summary>
    private static IServiceCollection AddSqlServerRepositories(this IServiceCollection services)
    {
        // SQL Server-optimized repositories (with server-side sorting/pagination)
        services.AddScoped<ISessionRepository, SqlServerRepos.SessionRepository>();
        services.AddScoped<IResponseRepository, SqlServerRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, SqlServerRepos.ParticipantRepository>();
        services.AddScoped<ILoginOtpRepository, SqlServerRepos.LoginOtpRepository>();
        services.AddScoped<IFacilitatorUserRepository, SqlServerRepos.FacilitatorUserRepository>();

        // Shared repositories (already optimized, work for both providers)
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IContributionCounterRepository, ContributionCounterRepository>();
        services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository>();
        services.AddScoped<ISessionGroupRepository, SessionGroupRepository>();
        services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository>();

        return services;
    }

    /// <summary>
    /// Registers standard repository implementations for SQLite and InMemory providers.
    /// </summary>
    private static IServiceCollection AddStandardRepositories(this IServiceCollection services)
    {
        // Use SQLite-specific implementations (inherit from base classes)
        services.AddScoped<ISessionRepository, SqliteRepos.SessionRepository>();
        services.AddScoped<IResponseRepository, SqliteRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, SqliteRepos.ParticipantRepository>();
        services.AddScoped<ILoginOtpRepository, SqliteRepos.LoginOtpRepository>();
        services.AddScoped<IFacilitatorUserRepository, SqliteRepos.FacilitatorUserRepository>();

        // Shared repositories (already optimized, work for both providers)
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IContributionCounterRepository, ContributionCounterRepository>();
        services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository>();
        services.AddScoped<ISessionGroupRepository, SessionGroupRepository>();
        services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created for the configured provider.
    /// For SQLite: Creates database file if not exists.
    /// For SQL Server: No action (must use manual scripts).
    /// For InMemory: No action needed.
    /// </summary>
    public static void EnsurePulseDatabase(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var useInMemory = configuration.GetValue<bool>("Pulse:UseInMemory");
        var databaseProvider = configuration.GetValue<string>("Pulse:DatabaseProvider") ?? "Sqlite";

        if (useInMemory)
        {
            // No initialization needed for InMemory
            return;
        }

        using var scope = serviceProvider.CreateScope();

        if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PulseSqlLiteDbContext>();
            dbContext.Database.EnsureCreated();
        }
        // For SQL Server, tables must be created manually using provided scripts
    }
}
