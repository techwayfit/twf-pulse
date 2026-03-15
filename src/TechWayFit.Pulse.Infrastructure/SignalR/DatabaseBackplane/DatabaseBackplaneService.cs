using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;

namespace TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane;

/// <summary>
/// Background service that polls the database for new SignalR messages from other servers
/// and broadcasts them to local clients. This enables SignalR to work across multiple servers
/// without Redis or Azure SignalR Service.
/// </summary>
public class DatabaseBackplaneService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseBackplaneService> _logger;
    private readonly string _serverId;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(500); // Poll every 500ms
    private readonly TimeSpan _messageRetention = TimeSpan.FromMinutes(5); // Keep messages for 5 minutes

    public DatabaseBackplaneService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseBackplaneService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Generate unique server ID (max 50 chars for database column)
        var serverIdRaw = $"{Environment.MachineName}_{Guid.NewGuid():N}";
        _serverId = serverIdRaw.Length > 50 ? serverIdRaw.Substring(0, 50) : serverIdRaw;
        
        _logger.LogInformation("Database backplane initialized for server: {ServerId}", _serverId);
    }

    public string ServerId => _serverId;

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    _logger.LogInformation("Database backplane service started");

  // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

  while (!stoppingToken.IsCancellationRequested)
   {
   try
     {
     await ProcessPendingMessages(stoppingToken);
      await CleanupOldMessages(stoppingToken);
 await Task.Delay(_pollInterval, stoppingToken);
     }
     catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
     {
   // Expected when shutting down
       break;
      }
   catch (Exception ex)
            {
        _logger.LogError(ex, "Error in database backplane polling loop");
       await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Back off on error
}
        }

 _logger.LogInformation("Database backplane service stopped");
    }

    private async Task ProcessPendingMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
     var dbContext = scope.ServiceProvider.GetRequiredService<IPulseDbContext>();
 
        // Get IHubContext dynamically to avoid circular dependency
        var hubContextType = Type.GetType("Microsoft.AspNetCore.SignalR.IHubContext`1[[TechWayFit.Pulse.Web.Hubs.WorkshopHub, TechWayFit.Pulse.Web]], Microsoft.AspNetCore.SignalR.Core");
     if (hubContextType == null)
        {
            _logger.LogWarning("Could not find WorkshopHub type for database backplane");
            return;
        }

 var hubContext = scope.ServiceProvider.GetService(hubContextType) as dynamic;
   if (hubContext == null)
        {
_logger.LogWarning("Could not resolve hub context for database backplane");
            return;
  }

  // Get unprocessed messages from OTHER servers (not our own)
        var messages = await dbContext.SignalRMessages
  .Where(m => !m.IsProcessed && m.ServerId != _serverId)
            .OrderBy(m => m.CreatedAt)
            .Take(100) // Process in batches
   .ToListAsync(cancellationToken);

      if (!messages.Any())
    return;

        foreach (var message in messages)
     {
     try
            {
        // Deserialize the payload
          var args = JsonSerializer.Deserialize<object[]>(message.PayloadJson);

      // Send to local clients in the group
       await hubContext.Clients.Group(message.GroupName)
    .SendCoreAsync(message.MethodName, args ?? Array.Empty<object>(), cancellationToken);

          // Mark as processed
     message.IsProcessed = true;
      message.ProcessedAt = DateTimeOffset.UtcNow;
          }
    catch (Exception ex)
{
   _logger.LogError(ex, "Failed to process message {MessageId} from server {ServerId}", 
 message.Id, message.ServerId);
  }
   }

    await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CleanupOldMessages(CancellationToken cancellationToken)
    {
    // Only cleanup every 30 seconds
if (DateTimeOffset.UtcNow.Second % 30 != 0)
     return;

        using var scope = _serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<IPulseDbContext>();

        var cutoff = DateTimeOffset.UtcNow.Subtract(_messageRetention);

        var oldMessages = await dbContext.SignalRMessages
     .Where(m => m.CreatedAt < cutoff)
     .ToListAsync(cancellationToken);

   if (oldMessages.Any())
     {
        dbContext.SignalRMessages.RemoveRange(oldMessages);
            await dbContext.SaveChangesAsync(cancellationToken);
_logger.LogInformation("Cleaned up {Count} old SignalR messages", oldMessages.Count);
        }
    }

    /// <summary>
    /// Store a SignalR message in the database for other servers to pick up
    /// </summary>
    public async Task StoreMessageAsync(string groupName, string methodName, object[] args)
    {
        try
        {
    using var scope = _serviceProvider.CreateScope();
     var dbContext = scope.ServiceProvider.GetRequiredService<IPulseDbContext>();

   var message = new SignalRMessage
            {
      GroupName = groupName,
 MethodName = methodName,
       PayloadJson = JsonSerializer.Serialize(args),
    ServerId = _serverId,
            CreatedAt = DateTimeOffset.UtcNow,
       IsProcessed = false
            };

            dbContext.SignalRMessages.Add(message);
    await dbContext.SaveChangesAsync();
        }
      catch (Exception ex)
{
 _logger.LogError(ex, "Failed to store SignalR message in database for group {GroupName}, method {Method}",
                groupName, methodName);
    }
    }
}
