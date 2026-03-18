using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;

namespace TechWayFit.Pulse.Infrastructure.Persistence.UnitOfWork;

public sealed class PulseUnitOfWork : IUnitOfWork
{
    private readonly DbContext _dbContext;

    public PulseUnitOfWork(IPulseDbContext dbContext)
    {
        _dbContext = (DbContext)dbContext;
    }

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteInTransactionAsync<object?>(
            async ct =>
            {
                await operation(ct);
                return null;
            },
            cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Reuse existing transaction when already running in one.
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
