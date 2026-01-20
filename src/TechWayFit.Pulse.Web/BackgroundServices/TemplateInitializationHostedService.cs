using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.BackgroundServices;

/// <summary>
/// Background service that initializes system templates from JSON files on application startup
/// </summary>
public sealed class TemplateInitializationHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TemplateInitializationHostedService> _logger;

    public TemplateInitializationHostedService(
        IServiceProvider serviceProvider,
        ILogger<TemplateInitializationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay to ensure app is fully started
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("Starting background template initialization...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ISessionTemplateService>();
            
            await templateService.InitializeSystemTemplatesAsync(stoppingToken);
            
            _logger.LogInformation("Background template initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during background template initialization");
        }
    }
}
