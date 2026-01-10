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
}
