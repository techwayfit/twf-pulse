using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Infrastructure.Caching;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring MariaDB database services.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Adds TechWayFit Pulse MariaDB database services.
    /// </summary>
    public static IServiceCollection AddPulseDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PulseDb")
 ?? throw new InvalidOperationException("ConnectionStrings:PulseDb is required in configuration.");

 // IApplicationCache is provider-agnostic
        services.AddSingleton<IApplicationCache, MemoryApplicationCache>();

        // Register MariaDB DbContext
    services.AddDbContext<PulseMariaDbContext>(options =>
            options.UseMySQL(connectionString, mySqlOptions =>
     {
                mySqlOptions.EnableRetryOnFailure(
     maxRetryCount: 3,
maxRetryDelay: TimeSpan.FromSeconds(5),
      errorNumbersToAdd: null);
 mySqlOptions.CommandTimeout(30);
     }));

        services.AddDbContextFactory<PulseMariaDbContext>(options =>
  options.UseMySQL(connectionString, mySqlOptions =>
  {
           mySqlOptions.EnableRetryOnFailure(
          maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(5),
       errorNumbersToAdd: null);
   mySqlOptions.CommandTimeout(30);
  }), ServiceLifetime.Scoped);

      services.AddScoped<IPulseDbContext>(sp => sp.GetRequiredService<PulseMariaDbContext>());

// Register repositories
        services.AddRepositories();

        return services;
  }

    /// <summary>
    /// Registers repository implementations with caching decorators.
  /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
// Session repository with caching
        services.AddScoped<SessionRepository>();
        services.AddScoped<ISessionRepository>(sp => new CachingSessionRepository(
     sp.GetRequiredService<SessionRepository>(),
            sp.GetRequiredService<IApplicationCache>()));

        // Activity repository with caching
    services.AddScoped<ActivityRepository<PulseMariaDbContext>>();
        services.AddScoped<IActivityRepository>(sp => new CachingActivityRepository(
   sp.GetRequiredService<ActivityRepository<PulseMariaDbContext>>(),
            sp.GetRequiredService<IApplicationCache>()));

    // Other repositories (no caching)
        services.AddScoped<IResponseRepository, ResponseRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
     services.AddScoped<ILoginOtpRepository, LoginOtpRepository>();
        services.AddScoped<IFacilitatorUserRepository, FacilitatorUserRepository>();
     services.AddScoped<IContributionCounterRepository, ContributionCounterRepository<PulseMariaDbContext>>();
        services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository<PulseMariaDbContext>>();
        services.AddScoped<ISessionGroupRepository, SessionGroupRepository<PulseMariaDbContext>>();
        services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository<PulseMariaDbContext>>();
        services.AddScoped<ISessionActivityMetadataRepository, SessionActivityMetadataRepository<PulseMariaDbContext>>();

        return services;
  }
}
