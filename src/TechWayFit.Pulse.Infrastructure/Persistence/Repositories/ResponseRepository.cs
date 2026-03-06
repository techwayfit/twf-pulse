using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB ResponseRepository implementation.
/// </summary>
public sealed class ResponseRepository : IResponseRepository
{
    private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

    public ResponseRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<Response?> GetByIdAsync(Guid responseId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Responses
          .AsNoTracking()
.FirstOrDefaultAsync(x => x.Id == responseId, cancellationToken);

        return record?.ToDomain();
    }

    public async Task AddAsync(Response response, CancellationToken cancellationToken = default)
    {
     await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.Responses.Add(response.ToRecord());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Response>> GetByActivityAsync(
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Responses
 .AsNoTracking()
            .Where(x => x.ActivityId == activityId)
            .OrderBy(x => x.CreatedAt)
          .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<int> CountByActivityAndParticipantAsync(
        Guid activityId,
        Guid participantId,
  CancellationToken cancellationToken = default)
    {
    await using var dbContext = await CreateDbContextAsync(cancellationToken);
        return await dbContext.Responses
    .AsNoTracking()
         .Where(x => x.ActivityId == activityId && x.ParticipantId == participantId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Response>> GetByParticipantAsync(
   Guid sessionId,
        Guid participantId,
     CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
   var records = await dbContext.Responses
            .AsNoTracking()
       .Where(x => x.SessionId == sessionId && x.ParticipantId == participantId)
  .OrderBy(x => x.CreatedAt)
       .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Response>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
  {
    await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var records = await dbContext.Responses
     .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
      .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task UpdatePayloadAsync(
        Guid responseId,
 string newPayload,
CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var record = await dbContext.Responses.FindAsync(new object[] { responseId }, cancellationToken);
  if (record is null)
        {
        throw new InvalidOperationException($"Response with ID {responseId} not found.");
        }

        record.PayloadJson = newPayload;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
