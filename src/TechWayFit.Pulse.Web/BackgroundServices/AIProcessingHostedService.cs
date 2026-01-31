using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Hubs;

namespace TechWayFit.Pulse.Web.BackgroundServices
{
    public class AIProcessingHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AIProcessingHostedService> _logger;

        public AIProcessingHostedService(IServiceProvider services, ILogger<AIProcessingHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AIProcessingHostedService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var queue = scope.ServiceProvider.GetRequiredService<IAIWorkQueue>();

                    if (queue is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    // Try to dequeue work from the concrete AIWorkQueue implementation
                    var concrete = queue as TechWayFit.Pulse.Infrastructure.AI.AIWorkQueue;
                    if (concrete == null)
                    {
                        // If queue isn't the in-memory implementation, wait and retry
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    if (!concrete.TryDequeue(out var item))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                    var participantAi = scope.ServiceProvider.GetRequiredService<IParticipantAIService>();
                    var facilitatorAi = scope.ServiceProvider.GetRequiredService<IFacilitatorAIService>();
                    var hub = scope.ServiceProvider.GetRequiredService<IHubContext<WorkshopHub, IWorkshopClient>>();

                    var (sessionId, activityId) = item;

                    var session = await sessionRepo.GetByIdAsync(sessionId, stoppingToken);
                    if (session == null)
                    {
                        _logger.LogWarning("AIProcessingHostedService: session {Session} not found", sessionId);
                        continue;
                    }

                    try
                    {
                        var (analysisResult, analysisTelemetry) = await participantAi.AnalyzeParticipantResponsesAsync(sessionId, activityId, stoppingToken);
                        var (promptResult, promptTelemetry) = await facilitatorAi.GenerateFacilitatorPromptAsync(sessionId, activityId, stoppingToken);

                        var payload = new
                        {
                            ActivityId = activityId,
                            Analysis = analysisResult,
                            FacilitatorPrompt = promptResult,
                            Telemetry = new
                            {
                                Analysis = analysisTelemetry,
                                Prompt = promptTelemetry
                            }
                        };

                        await hub.Clients.Group(session.Code).DashboardUpdated(new DashboardUpdatedEvent(
                            session.Code,
                            activityId,
                            "AIInsight",
                            payload,
                            DateTimeOffset.UtcNow));

                        _logger.LogInformation("AIProcessingHostedService: processed AI insight for session {Session} activity {Activity}", session.Code, activityId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AIProcessingHostedService failed to process AI task for session {Session} activity {Activity}", session.Code, activityId);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in AIProcessingHostedService loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("AIProcessingHostedService stopping");
        }
    }
}
