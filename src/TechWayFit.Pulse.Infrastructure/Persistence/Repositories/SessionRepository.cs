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
        _dbContext.Sessions.Update(session.ToRecord());
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
}
