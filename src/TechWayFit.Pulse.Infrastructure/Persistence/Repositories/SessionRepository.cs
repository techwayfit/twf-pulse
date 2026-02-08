using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly PulseDbContext _dbContext;

    public SessionRepository(PulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<Session?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        return record?.ToDomain();
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        _dbContext.Sessions.Add(session.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        // Find the existing tracked entity or attach if not tracked
        var existingRecord = await _dbContext.Sessions.FindAsync(new object[] { session.Id }, cancellationToken);

        if (existingRecord == null)
        {
            // Entity doesn't exist in database - this shouldn't happen for Update
            throw new InvalidOperationException($"Session with ID {session.Id} not found.");
        }

        // Update the existing record's properties from the domain entity
        var updatedRecord = session.ToRecord();

        // Copy all properties to the tracked entity
        _dbContext.Entry(existingRecord).CurrentValues.SetValues(updatedRecord);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetByFacilitatorUserIdAsync(
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId)
            .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records.OrderByDescending(x => x.CreatedAt).ToList();

        return sortedRecords.Select(r => r.ToDomain()).ToList();
    }

    public async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetByFacilitatorUserIdPaginatedAsync(
        Guid facilitatorUserId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query.ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var sessions = sortedRecords.Select(r => r.ToDomain()).ToList();

        return (sessions, totalCount);
    }

    public async Task<IReadOnlyCollection<Session>> GetByGroupAsync(
        Guid? groupId,
        Guid facilitatorUserId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.FacilitatorUserId == facilitatorUserId && x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records.OrderByDescending(x => x.CreatedAt).ToList();

        return sortedRecords.Select(r => r.ToDomain()).ToList();
    }
}
