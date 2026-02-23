using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
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

        services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseMariaDbContext>());

        services.AddMariaDbRepositories();

        return services;
    }

    /// <summary>
    /// Registers SQL Server-optimized repository implementations.
    /// </summary>
    private static IServiceCollection AddSqlServerRepositories(this IServiceCollection services)
    {
        services.AddScoped<ISessionRepository, SqlServerRepos.SessionRepository>();
    services.AddScoped<IResponseRepository, SqlServerRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, SqlServerRepos.ParticipantRepository>();
services.AddScoped<ILoginOtpRepository, SqlServerRepos.LoginOtpRepository>();
        services.AddScoped<IFacilitatorUserRepository, SqlServerRepos.FacilitatorUserRepository>();

  services.AddScoped<IActivityRepository, ActivityRepository>();
      services.AddScoped<IContributionCounterRepository, ContributionCounterRepository>();
        services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository>();
 services.AddScoped<ISessionGroupRepository, SessionGroupRepository>();
        services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository>();

        return services;
    }

    /// <summary>
    /// Registers MariaDB-optimized repository implementations.
    /// </summary>
    private static IServiceCollection AddMariaDbRepositories(this IServiceCollection services)
    {
        services.AddScoped<ISessionRepository, MariaDbRepos.SessionRepository>();
        services.AddScoped<IResponseRepository, MariaDbRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, MariaDbRepos.ParticipantRepository>();
    services.AddScoped<ILoginOtpRepository, MariaDbRepos.LoginOtpRepository>();
     services.AddScoped<IFacilitatorUserRepository, MariaDbRepos.FacilitatorUserRepository>();

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
services.AddScoped<ISessionRepository, SqliteRepos.SessionRepository>();
        services.AddScoped<IResponseRepository, SqliteRepos.ResponseRepository>();
        services.AddScoped<IParticipantRepository, SqliteRepos.ParticipantRepository>();
        services.AddScoped<ILoginOtpRepository, SqliteRepos.LoginOtpRepository>();
     services.AddScoped<IFacilitatorUserRepository, SqliteRepos.FacilitatorUserRepository>();

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
