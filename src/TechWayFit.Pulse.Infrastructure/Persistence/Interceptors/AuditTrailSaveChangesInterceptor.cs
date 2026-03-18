using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Interceptors;

public sealed class AuditTrailSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AuditTrailSaveChangesInterceptor> _logger;

    public AuditTrailSaveChangesInterceptor(ILogger<AuditTrailSaveChangesInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        LogAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        LogAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void LogAuditEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker
            .Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => !entry.Metadata.IsOwned())
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        foreach (var entry in entries)
        {
            var payload = BuildPayload(entry);
            _logger.LogInformation(
                "AuditTrail Entity={Entity} State={State} Changes={Changes}",
                entry.Metadata.ClrType.Name,
                entry.State,
                JsonSerializer.Serialize(payload));
        }
    }

    private static object BuildPayload(EntityEntry entry)
    {
        var primaryKeys = entry.Properties
            .Where(property => property.Metadata.IsPrimaryKey())
            .ToDictionary(property => property.Metadata.Name, property => property.CurrentValue);

        var changes = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            if (entry.State == EntityState.Modified && !property.IsModified)
            {
                continue;
            }

            changes[property.Metadata.Name] = entry.State switch
            {
                EntityState.Added => property.CurrentValue,
                EntityState.Deleted => property.OriginalValue,
                _ => new
                {
                    From = property.OriginalValue,
                    To = property.CurrentValue
                }
            };
        }

        return new
        {
            Keys = primaryKeys,
            Changes = changes,
            TimestampUtc = DateTimeOffset.UtcNow
        };
    }
}
