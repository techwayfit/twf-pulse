using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Application.Services;

/// <inheritdoc cref="ISessionActivityMetadataService"/>
public sealed class SessionActivityMetadataService : ISessionActivityMetadataService
{
    private readonly ISessionActivityMetadataRepository _repository;

    public SessionActivityMetadataService(ISessionActivityMetadataRepository repository)
    {
        _repository = repository;
    }

    public async Task<string?> GetValueAsync(
        Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetAsync(sessionId, activityId, key, cancellationToken);
        return record?.Value;
    }

    public Task SetValueAsync(
        Guid sessionId, Guid activityId, string key, string value, CancellationToken cancellationToken = default)
        => _repository.UpsertAsync(sessionId, activityId, key, value, cancellationToken);

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(
        Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetAllAsync(sessionId, activityId, cancellationToken);
        return records.ToDictionary(r => r.Key, r => r.Value);
    }

    public Task DeleteAsync(
        Guid sessionId, Guid activityId, string key, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(sessionId, activityId, key, cancellationToken);

    public Task DeleteAllForActivityAsync(
        Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
        => _repository.DeleteAllForActivityAsync(sessionId, activityId, cancellationToken);
}
