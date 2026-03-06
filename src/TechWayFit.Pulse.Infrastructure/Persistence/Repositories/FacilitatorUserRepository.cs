using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence.Mapping;
using TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// MariaDB FacilitatorUserRepository implementation.
/// </summary>
public sealed class FacilitatorUserRepository : IFacilitatorUserRepository
{
    private readonly IDbContextFactory<PulseMariaDbContext> _dbContextFactory;

    public FacilitatorUserRepository(IDbContextFactory<PulseMariaDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<PulseMariaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task<FacilitatorUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
  await using var dbContext = await CreateDbContextAsync(cancellationToken);
     var record = await dbContext.FacilitatorUsers
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<FacilitatorUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
     var record = await dbContext.FacilitatorUsers
       .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<FacilitatorUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
 await using var dbContext = await CreateDbContextAsync(cancellationToken);
 var records = await dbContext.FacilitatorUsers
          .AsNoTracking()
   .OrderBy(x => x.CreatedAt)
       .ToListAsync(cancellationToken);

        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
  {
     await using var dbContext = await CreateDbContextAsync(cancellationToken);
 return await dbContext.FacilitatorUsers
   .AsNoTracking()
      .AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task AddAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
   dbContext.FacilitatorUsers.Add(user.ToRecord());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FacilitatorUser user, CancellationToken cancellationToken = default)
  {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
     var record = await dbContext.FacilitatorUsers.FindAsync(new object[] { user.Id }, cancellationToken);
        if (record is null)
        {
throw new InvalidOperationException($"FacilitatorUser with ID {user.Id} not found.");
  }

        record.Email = user.Email;
   record.DisplayName = user.DisplayName;
 record.LastLoginAt = user.LastLoginAt;

      await dbContext.SaveChangesAsync(cancellationToken);
    }
}
