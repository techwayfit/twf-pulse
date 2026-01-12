using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

public sealed class ResponseRepository : IResponseRepository
{
    private readonly PulseDbContext _dbContext;

    public ResponseRepository(PulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Response response, CancellationToken cancellationToken = default)
    {
        _dbContext.Responses.Add(response.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Response>> GetByActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Responses
            .AsNoTracking()
            .Where(x => x.ActivityId == activityId)
            .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records.OrderBy(x => x.CreatedAt).ToList();

        return sortedRecords.Select(record => record.ToDomain()).ToList();
    }

    public Task<int> CountByActivityAndParticipantAsync(
        Guid activityId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Responses
            .AsNoTracking()
            .Where(x => x.ActivityId == activityId && x.ParticipantId == participantId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Response>> GetByParticipantAsync(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Responses
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId && x.ParticipantId == participantId)
            .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records.OrderBy(x => x.CreatedAt).ToList();

        return sortedRecords.Select(record => record.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Response>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Responses
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        // Sort on the client side to avoid SQLite DateTimeOffset ordering issues
        var sortedRecords = records.OrderBy(x => x.CreatedAt).ToList();

        return sortedRecords.Select(record => record.ToDomain()).ToList();
    }
}
