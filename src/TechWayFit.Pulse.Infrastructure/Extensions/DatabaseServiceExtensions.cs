using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Infrastructure.Caching;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using SqlServerRepos = TechWayFit.Pulse.Infrastructure.Persistence.SqlServer.Repositories;
using SqliteRepos = TechWayFit.Pulse.Infrastructure.Persistence.Sqlite.Repositories;
using MariaDbRepos = TechWayFit.Pulse.Infrastructure.Persistence.MariaDb.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;
using TechWayFit.Pulse.Infrastructure.Persistence.Sqlite;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

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

        // IApplicationCache is provider-agnostic — register once here.
        // MemoryApplicationCache wraps IMemoryCache (registered via AddMemoryCache in Program.cs).
        // To switch to a distributed cache (Redis, etc.), replace this registration only.
        services.AddSingleton<IApplicationCache, MemoryApplicationCache>();

        if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
        {
     services.AddPulseInMemoryDatabase();
     }
        else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddPulseSqlServerDatabase(connectionString);
        }
   else if (databaseProvider.Equals("MariaDB", StringComparison.OrdinalIgnoreCase) ||
       databaseProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
        {
 services.AddPulseMariaDbDatabase(connectionString);
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
        services.AddDbContextFactory<PulseSqlLiteDbContext>(options =>
        {
            options.UseInMemoryDatabase("Pulse");
        }, ServiceLifetime.Scoped);

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseSqlLiteDbContext>());

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
        services.AddDbContextFactory<PulseSqlLiteDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly("TechWayFit.Pulse.Web");
            });
        }, ServiceLifetime.Scoped);

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseSqlLiteDbContext>());

        services.AddStandardRepositories();

        return services;
    }

    /// <summary>
    /// Adds SQL Server database with appropriate configuration and retry logic.
    /// NOTE: EF Migrations are DISABLED for SQL Server.
    /// All schema changes must be applied via SQL scripts in Infrastructure/Scripts/MSQL/
    /// </summary>
    private static IServiceCollection AddPulseSqlServerDatabase(
        this IServiceCollection services,
        string connectionString)
    {
   services.AddDbContext<PulseSqlServerDbContext>(options =>
        {
options.UseSqlServer(connectionString, sqlServerOptions =>
  {
                sqlServerOptions.EnableRetryOnFailure(
    maxRetryCount: 3,
      maxRetryDelay: TimeSpan.FromSeconds(5),
              errorNumbersToAdd: null);

                sqlServerOptions.CommandTimeout(30);

  sqlServerOptions.MigrationsHistoryTable("__MigrationHistory", "pulse");
      });
  });
        services.AddDbContextFactory<PulseSqlServerDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                sqlServerOptions.CommandTimeout(30);

                sqlServerOptions.MigrationsHistoryTable("__MigrationHistory", "pulse");
            });
        }, ServiceLifetime.Scoped);

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseSqlServerDbContext>());

        services.AddSqlServerRepositories();

        return services;
    }

    /// <summary>
    /// Adds MariaDB/MySQL database using Oracle's official provider.
/// Fully compatible with .NET 10 / EF Core 10.
    /// NOTE: EF Migrations are DISABLED for MariaDB.
    /// All schema changes must be applied via SQL scripts in Infrastructure/Scripts/MariaDB/
    /// </summary>
    private static IServiceCollection AddPulseMariaDbDatabase(
  this IServiceCollection services,
  string connectionString)
    {
        services.AddDbContext<PulseMariaDbContext>(options =>
        {
        // Oracle's official MySQL provider - fully compatible with EF Core 10
options.UseMySQL(connectionString, mySqlOptions =>
    {
           mySqlOptions.EnableRetryOnFailure(
          maxRetryCount: 3,
         maxRetryDelay: TimeSpan.FromSeconds(5),
    errorNumbersToAdd: null);

        mySqlOptions.CommandTimeout(30);

      mySqlOptions.MigrationsHistoryTable("__MigrationHistory", "pulse");
            });
        });
        services.AddDbContextFactory<PulseMariaDbContext>(options =>
        {
            options.UseMySQL(connectionString, mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                mySqlOptions.CommandTimeout(30);

                mySqlOptions.MigrationsHistoryTable("__MigrationHistory", "pulse");
            });
        }, ServiceLifetime.Scoped);

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseMariaDbContext>());

        services.AddMariaDbRepositories();

        return services;
    }

    /// <summary>
    /// Registers SQL Server-optimized repository implementations.
    /// </summary>
    private static IServiceCollection AddSqlServerRepositories(this IServiceCollection services)
    {
        services.AddScoped<SqlServerRepos.SessionRepository>();
        services.AddScoped<ISessionRepository>(sp => new CachingSessionRepository(
            sp.GetRequiredService<SqlServerRepos.SessionRepository>(),
            sp.GetRequiredService<IApplicationCache>()));
        services.AddScoped<IResponseRepository, SqlServerRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, SqlServerRepos.ParticipantRepository>();
        services.AddScoped<ILoginOtpRepository, SqlServerRepos.LoginOtpRepository>();
        services.AddScoped<IFacilitatorUserRepository, SqlServerRepos.FacilitatorUserRepository>();

        services.AddScoped<ActivityRepository<PulseSqlServerDbContext>>();
        services.AddScoped<IActivityRepository>(sp => new CachingActivityRepository(
            sp.GetRequiredService<ActivityRepository<PulseSqlServerDbContext>>(),
            sp.GetRequiredService<IApplicationCache>()));
        services.AddScoped<IContributionCounterRepository, ContributionCounterRepository<PulseSqlServerDbContext>>();
        services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository<PulseSqlServerDbContext>>();
        services.AddScoped<ISessionGroupRepository, SessionGroupRepository<PulseSqlServerDbContext>>();
        services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository<PulseSqlServerDbContext>>();
        services.AddScoped<ISessionActivityMetadataRepository, SessionActivityMetadataRepository<PulseSqlServerDbContext>>();

        return services;
    }

    /// <summary>
    /// Registers MariaDB-optimized repository implementations.
    /// </summary>
    private static IServiceCollection AddMariaDbRepositories(this IServiceCollection services)
    {
        services.AddScoped<MariaDbRepos.SessionRepository>();
        services.AddScoped<ISessionRepository>(sp => new CachingSessionRepository(
            sp.GetRequiredService<MariaDbRepos.SessionRepository>(),
            sp.GetRequiredService<IApplicationCache>()));
        services.AddScoped<IResponseRepository, MariaDbRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, MariaDbRepos.ParticipantRepository>();
        services.AddScoped<ILoginOtpRepository, MariaDbRepos.LoginOtpRepository>();
        services.AddScoped<IFacilitatorUserRepository, MariaDbRepos.FacilitatorUserRepository>();

        services.AddScoped<ActivityRepository<PulseMariaDbContext>>();
        services.AddScoped<IActivityRepository>(sp => new CachingActivityRepository(
            sp.GetRequiredService<ActivityRepository<PulseMariaDbContext>>(),
            sp.GetRequiredService<IApplicationCache>()));
        services.AddScoped<IContributionCounterRepository, ContributionCounterRepository<PulseMariaDbContext>>();
        services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository<PulseMariaDbContext>>();
        services.AddScoped<ISessionGroupRepository, SessionGroupRepository<PulseMariaDbContext>>();
        services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository<PulseMariaDbContext>>();
        services.AddScoped<ISessionActivityMetadataRepository, SessionActivityMetadataRepository<PulseMariaDbContext>>();

        return services;
    }

 /// <summary>
    /// Registers standard repository implementations for SQLite and InMemory providers.
  /// </summary>
    private static IServiceCollection AddStandardRepositories(this IServiceCollection services)
    {
        services.AddScoped<SqliteRepos.SessionRepository>();
        services.AddScoped<ISessionRepository>(sp => new CachingSessionRepository(
            sp.GetRequiredService<SqliteRepos.SessionRepository>(),
            sp.GetRequiredService<IApplicationCache>()));
        services.AddScoped<IResponseRepository, SqliteRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, SqliteRepos.ParticipantRepository>();
        services.AddScoped<ILoginOtpRepository, SqliteRepos.LoginOtpRepository>();
        services.AddScoped<IFacilitatorUserRepository, SqliteRepos.FacilitatorUserRepository>();

        services.AddScoped<ActivityRepository<PulseSqlLiteDbContext>>();
        services.AddScoped<IActivityRepository>(sp => new CachingActivityRepository(
            sp.GetRequiredService<ActivityRepository<PulseSqlLiteDbContext>>(),
            sp.GetRequiredService<IApplicationCache>()));
        services.AddScoped<IContributionCounterRepository, ContributionCounterRepository<PulseSqlLiteDbContext>>();
        services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository<PulseSqlLiteDbContext>>();
        services.AddScoped<ISessionGroupRepository, SessionGroupRepository<PulseSqlLiteDbContext>>();
        services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository<PulseSqlLiteDbContext>>();
        services.AddScoped<ISessionActivityMetadataRepository, SessionActivityMetadataRepository<PulseSqlLiteDbContext>>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created for the configured provider.
    /// For SQLite: Creates database file if not exists.
  /// For SQL Server: No action (must use manual scripts).
    /// For MariaDB: No action (must use manual scripts).
    /// For InMemory: No action needed.
    /// </summary>
    public static void EnsurePulseDatabase(this IServiceProvider serviceProvider, IConfiguration configuration)
 {
        var useInMemory = configuration.GetValue<bool>("Pulse:UseInMemory");
   var databaseProvider = configuration.GetValue<string>("Pulse:DatabaseProvider") ?? "Sqlite";

        if (useInMemory)
      {
     return;
        }

        using var scope = serviceProvider.CreateScope();

        if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
     {
      var dbContext = scope.ServiceProvider.GetRequiredService<PulseSqlLiteDbContext>();
 dbContext.Database.EnsureCreated();
        }
        // For SQL Server and MariaDB, tables must be created manually using provided scripts
    }
}
