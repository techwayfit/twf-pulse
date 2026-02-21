using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// Shared SessionTemplateRepository implementation.
/// </summary>
public class SessionTemplateRepository : ISessionTemplateRepository
{
    protected readonly IPulseDbContext _dbContext;

    public SessionTemplateRepository(IPulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SessionTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.SessionTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record == null ? null : MapToDomain(record);
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SessionTemplates
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetByCategoryAsync(TemplateCategory category, CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SessionTemplates
            .AsNoTracking()
            .Where(x => x.Category == (int)category)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SessionTemplates
            .AsNoTracking()
            .Where(x => x.IsSystemTemplate)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SessionTemplate>> GetUserTemplatesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SessionTemplates
            .AsNoTracking()
            .Where(x => !x.IsSystemTemplate && x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    public async Task AddAsync(SessionTemplate template, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(template);
        await _dbContext.SessionTemplates.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SessionTemplate template, CancellationToken cancellationToken = default)
    {
        var record = MapToRecord(template);
        _dbContext.SessionTemplates.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.SessionTemplates.FindAsync(new object[] { id }, cancellationToken);
        if (record != null)
        {
            _dbContext.SessionTemplates.Remove(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
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
