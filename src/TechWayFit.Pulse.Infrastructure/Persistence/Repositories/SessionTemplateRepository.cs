using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared SessionTemplateRepository implementation for all providers.
/// </summary>
public class SessionTemplateRepository<TContext> : ISessionTemplateRepository
    where TContext : DbContext, IPulseDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    public SessionTemplateRepository(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<SessionTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.SessionTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.SessionTemplates
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetByCategoryAsync(TemplateCategory category, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.SessionTemplates
            .AsNoTracking()
            .Where(x => x.Category == (int)category)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.SessionTemplates
            .AsNoTracking()
            .Where(x => x.IsSystemTemplate)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetUserTemplatesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.SessionTemplates
            .AsNoTracking()
            .Where(x => !x.IsSystemTemplate && x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task AddAsync(SessionTemplate template, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(template);
        await dbContext.SessionTemplates.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SessionTemplate template, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = MapToRecord(template);
        dbContext.SessionTemplates.Update(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.SessionTemplates.FindAsync(new object[] { id }, cancellationToken);
        if (record != null)
        {
            dbContext.SessionTemplates.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    protected static SessionTemplate MapToDomain(SessionTemplateRecord record)
    {
        return new SessionTemplate(
            record.Id,
            record.Name,
            record.Description,
            (TemplateCategory)record.Category,
            record.IconEmoji,
            record.ConfigJson,
            record.IsSystemTemplate,
            record.CreatedByUserId,
            record.CreatedAt,
            record.UpdatedAt);
    }

    protected static SessionTemplateRecord MapToRecord(SessionTemplate template)
    {
        return new SessionTemplateRecord
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = (int)template.Category,
            IconEmoji = template.IconEmoji,
            ConfigJson = template.ConfigJson,
            IsSystemTemplate = template.IsSystemTemplate,
            CreatedByUserId = template.CreatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
