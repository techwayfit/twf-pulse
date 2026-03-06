using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;
using TechWayFit.Pulse.BackOffice.Core.Services;

namespace TechWayFit.Pulse.BackOffice.Core;

public static class BackOfficeCoreServiceExtensions
{
    /// <summary>
    /// Registers all BackOffice.Core services including MariaDB DbContext.
    /// Call from <c>Program.cs</c> in TechWayFit.Pulse.BackOffice.
    /// </summary>
    public static IServiceCollection AddBackOfficeCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PulseDb")
      ?? throw new InvalidOperationException("ConnectionStrings:PulseDb is required.");

  // Register MariaDB DbContext
        services.AddDbContext<BackOfficeMariaDbContext>(options =>
         options.UseMySQL(connectionString, mySqlOptions =>
            {
         mySqlOptions.EnableRetryOnFailure(
         maxRetryCount: 3,
       maxRetryDelay: TimeSpan.FromSeconds(5),
       errorNumbersToAdd: null);
 mySqlOptions.CommandTimeout(30);
}));

     // Register services
services.AddScoped<IAuditLogService, AuditLogService>();
   services.AddScoped<IBackOfficeUserService, BackOfficeUserService>();
      services.AddScoped<IBackOfficeSessionService, BackOfficeSessionService>();
    services.AddScoped<IBackOfficeAuthService, BackOfficeAuthService>();

        // Cache management — calls main app's internal API via named HttpClient.
        // Requires MainApp:BaseUrl and MainApp:BackOfficeApiToken in appsettings.
   services.AddHttpClient(BackOfficeCacheService.HttpClientName, (sp, client) =>
        {
   var cfg = sp.GetRequiredService<IConfiguration>();
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
