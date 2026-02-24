using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;

namespace TechWayFit.Pulse.Web.Api;

public interface IParticipantTokenStore
{
    ParticipantAuth Create(Guid sessionId, Guid participantId);

    /// <summary>Cache-only lookup. Returns false if not cached (use TryGetAsync for DB fallback).</summary>
    bool TryGet(Guid sessionId, Guid participantId, out ParticipantAuth auth);

    /// <summary>Cache-first lookup with DB fallback. Always use this from async code.</summary>
    Task<ParticipantAuth?> TryGetAsync(Guid sessionId, Guid participantId);

    /// <summary>Cache-only validation. Use IsValidAsync from async code.</summary>
    bool IsValid(Guid sessionId, Guid participantId, string token);

    /// <summary>Cache-first validation with DB fallback. Always use this from async code.</summary>
    Task<bool> IsValidAsync(Guid sessionId, Guid participantId, string token);
}

public sealed record ParticipantAuth(Guid ParticipantId, string Token, DateTimeOffset IssuedAt);

public sealed class ParticipantTokenStore : IParticipantTokenStore
{
    // In-memory cache for performance, with fallback to database
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(8);
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    public ParticipantTokenStore(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public ParticipantAuth Create(Guid sessionId, Guid participantId)
    {
        var auth = new ParticipantAuth(participantId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
        CacheToken(sessionId, participantId, auth);
        
        // Note: Token will be persisted to database when participant is created/updated
        return auth;
    }

    /// <inheritdoc/>
    public bool TryGet(Guid sessionId, Guid participantId, out ParticipantAuth auth)
    {
        var cacheKey = GetCacheKey(sessionId, participantId);
        if (_cache.TryGetValue(cacheKey, out ParticipantAuth? cachedAuth) && cachedAuth is not null)
        {
            auth = cachedAuth;
            return true;
        }
        auth = default!;
        return false;
    }

    /// <inheritdoc/>
    public async Task<ParticipantAuth?> TryGetAsync(Guid sessionId, Guid participantId)
    {
        var cacheKey = GetCacheKey(sessionId, participantId);

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out ParticipantAuth? cachedAuth) && cachedAuth is not null)
        {
            return cachedAuth;
        }

        // Fallback to database using async to avoid DbContext threading issues
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IPulseDbContext>();

            var participant = await dbContext.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.Id == participantId);

            if (participant?.Token != null)
            {
                var auth = new ParticipantAuth(participantId, participant.Token, participant.JoinedAt);
                CacheToken(sessionId, participantId, auth);
                return auth;
            }
        }
        catch
        {
            // If database query fails, return null
        }

        return null;
    }

    /// <inheritdoc/>
    public bool IsValid(Guid sessionId, Guid participantId, string token)
    {
        if (!TryGet(sessionId, participantId, out var auth))
        {
            return false;
        }
        return string.Equals(auth.Token, token, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public async Task<bool> IsValidAsync(Guid sessionId, Guid participantId, string token)
    {
        var auth = await TryGetAsync(sessionId, participantId);
        if (auth is null) return false;
        return string.Equals(auth.Token, token, StringComparison.Ordinal);
    }

    private static string GetCacheKey(Guid sessionId, Guid participantId)
    {
        return $"participant-token:{sessionId:N}:{participantId:N}";
    }

    private void CacheToken(Guid sessionId, Guid participantId, ParticipantAuth auth)
    {
        var cacheKey = GetCacheKey(sessionId, participantId);
        _cache.Set(cacheKey, auth, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });
    }
}
