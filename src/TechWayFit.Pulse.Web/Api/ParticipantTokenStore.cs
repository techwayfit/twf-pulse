using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;

namespace TechWayFit.Pulse.Web.Api;

public interface IParticipantTokenStore
{
    ParticipantAuth Create(Guid sessionId, Guid participantId);

    /// <summary>Cache-only lookup. Returns false if not cached (use TryGetAsync for distributed/DB fallback).</summary>
    bool TryGet(Guid sessionId, Guid participantId, out ParticipantAuth auth);

    /// <summary>Cache-first lookup with distributed cache and DB fallback. Always use this from async code.</summary>
    Task<ParticipantAuth?> TryGetAsync(Guid sessionId, Guid participantId);

    /// <summary>Cache-first validation with distributed cache and DB fallback.</summary>
    Task<bool> IsValidAsync(Guid sessionId, Guid participantId, string token);
}

public sealed record ParticipantAuth(Guid ParticipantId, string Token, DateTimeOffset IssuedAt);

public sealed class ParticipantTokenStore : IParticipantTokenStore
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(8);
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache _distributedCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ParticipantTokenStore> _logger;

    public ParticipantTokenStore(
        IMemoryCache cache,
        IDistributedCache distributedCache,
        IServiceScopeFactory scopeFactory,
        ILogger<ParticipantTokenStore> logger)
    {
        _cache = cache;
        _distributedCache = distributedCache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ParticipantAuth Create(Guid sessionId, Guid participantId)
    {
        var auth = new ParticipantAuth(participantId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
        CacheToken(sessionId, participantId, auth);
        return auth;
    }

    public bool TryGet(Guid sessionId, Guid participantId, out ParticipantAuth auth)
    {
        var cacheKey = GetMemoryCacheKey(sessionId, participantId);
        if (_cache.TryGetValue(cacheKey, out ParticipantAuth? cachedAuth) && cachedAuth is not null)
        {
            auth = cachedAuth;
            return true;
        }

        auth = default!;
        return false;
    }

    public async Task<ParticipantAuth?> TryGetAsync(Guid sessionId, Guid participantId)
    {
        if (TryGet(sessionId, participantId, out var cached))
        {
            return cached;
        }

        var distributedKey = GetDistributedCacheKey(sessionId, participantId);
        try
        {
            var distributedPayload = await _distributedCache.GetStringAsync(distributedKey);
            if (!string.IsNullOrWhiteSpace(distributedPayload))
            {
                var distributedAuth = JsonSerializer.Deserialize<ParticipantAuth>(distributedPayload);
                if (distributedAuth is not null)
                {
                    CacheTokenInMemory(sessionId, participantId, distributedAuth);
                    return distributedAuth;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read participant token from distributed cache for SessionId={SessionId} ParticipantId={ParticipantId}", sessionId, participantId);
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IPulseDbContext>();

            var participant = await dbContext.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.Id == participantId);

            if (participant is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(participant.Token))
            {
                participant.Token = Guid.NewGuid().ToString("N");

                using var updateScope = _scopeFactory.CreateScope();
                var updateContext = updateScope.ServiceProvider.GetRequiredService<IPulseDbContext>();
                var participantToUpdate = await updateContext.Participants
                    .FirstOrDefaultAsync(p => p.Id == participantId);

                if (participantToUpdate != null)
                {
                    participantToUpdate.Token = participant.Token;
                    await updateContext.SaveChangesAsync();
                }
            }

            var auth = new ParticipantAuth(participantId, participant.Token!, participant.JoinedAt);
            CacheToken(sessionId, participantId, auth);
            return auth;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load participant token from database for SessionId={SessionId} ParticipantId={ParticipantId}", sessionId, participantId);
            return null;
        }
    }

    public async Task<bool> IsValidAsync(Guid sessionId, Guid participantId, string token)
    {
        var auth = await TryGetAsync(sessionId, participantId);
        if (auth is null)
        {
            return false;
        }

        return string.Equals(auth.Token, token, StringComparison.Ordinal);
    }

    private static string GetMemoryCacheKey(Guid sessionId, Guid participantId)
    {
        return $"participant-token:{sessionId:N}:{participantId:N}";
    }

    private static string GetDistributedCacheKey(Guid sessionId, Guid participantId)
    {
        return $"participant-token-distributed:{sessionId:N}:{participantId:N}";
    }

    private void CacheToken(Guid sessionId, Guid participantId, ParticipantAuth auth)
    {
        CacheTokenInMemory(sessionId, participantId, auth);

        var distributedKey = GetDistributedCacheKey(sessionId, participantId);
        try
        {
            _distributedCache.SetString(
                distributedKey,
                JsonSerializer.Serialize(auth),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write participant token to distributed cache for SessionId={SessionId} ParticipantId={ParticipantId}", sessionId, participantId);
        }
    }

    private void CacheTokenInMemory(Guid sessionId, Guid participantId, ParticipantAuth auth)
    {
        var cacheKey = GetMemoryCacheKey(sessionId, participantId);
        _cache.Set(cacheKey, auth, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1
        });
    }
}
