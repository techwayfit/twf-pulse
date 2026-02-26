using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository for <see cref="SessionActivityMetadata"/>.
/// Shared across all database providers (SQLite, SQL Server, MariaDB).
/// </summary>
public class SessionActivityMetadataRepository<TContext> : ISessionActivityMetadataRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _factory;

    public SessionActivityMetadataRepository(IDbContextFactory<TContext> factory)
    {
        _factory = factory;
    }

    private async Task<TContext> CreateDbContextAsync(CancellationToken ct = default)
        => await _factory.CreateDbContextAsync(ct);

    public async Task<SessionActivityMetadata?> GetAsync(
        Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var record = await db.SessionActivityMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.SessionId == sessionId && x.ActivityId == activityId && x.Key == key,
                cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<SessionActivityMetadata>> GetAllAsync(
        Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var records = await db.SessionActivityMetadata
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId && x.ActivityId == activityId)
            .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task UpsertAsync(
        Guid sessionId, Guid activityId, string key, string value,
        CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var existing = await db.SessionActivityMetadata
            .FirstOrDefaultAsync(
                x => x.SessionId == sessionId && x.ActivityId == activityId && x.Key == key,
                cancellationToken);

        if (existing is not null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            await db.SessionActivityMetadata.AddAsync(new SessionActivityMetadataRecord
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                ActivityId = activityId,
                Key = key,
                Value = value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var record = await db.SessionActivityMetadata
            .FirstOrDefaultAsync(
                x => x.SessionId == sessionId && x.ActivityId == activityId && x.Key == key,
                cancellationToken);

        if (record is not null)
        {
            db.SessionActivityMetadata.Remove(record);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAllForActivityAsync(
        Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var records = await db.SessionActivityMetadata
            .Where(x => x.SessionId == sessionId && x.ActivityId == activityId)
            .ToListAsync(cancellationToken);

        if (records.Count > 0)
        {
            db.SessionActivityMetadata.RemoveRange(records);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAllForSessionAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var records = await db.SessionActivityMetadata
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (records.Count > 0)
        {
            db.SessionActivityMetadata.RemoveRange(records);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

// ── Mapping helpers (internal to this namespace) ──────────────────────────────
file static class SessionActivityMetadataMapper
{
    internal static SessionActivityMetadata ToDomain(this SessionActivityMetadataRecord r)
        => new(r.Id, r.SessionId, r.ActivityId, r.Key, r.Value, r.CreatedAt, r.UpdatedAt);
}
