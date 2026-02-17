using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence;

namespace TechWayFit.Pulse.Web.Api;

public interface IParticipantTokenStore
{
    ParticipantAuth Create(Guid sessionId, Guid participantId);

    bool TryGet(Guid sessionId, Guid participantId, out ParticipantAuth auth);

    bool IsValid(Guid sessionId, Guid participantId, string token);
}

public sealed record ParticipantAuth(Guid ParticipantId, string Token, DateTimeOffset IssuedAt);

public sealed class ParticipantTokenStore : IParticipantTokenStore
{
    // In-memory cache for performance, with fallback to database
    private readonly ConcurrentDictionary<(Guid SessionId, Guid ParticipantId), ParticipantAuth> _cache = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public ParticipantTokenStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public ParticipantAuth Create(Guid sessionId, Guid participantId)
    {
        var auth = new ParticipantAuth(participantId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
        _cache.AddOrUpdate((sessionId, participantId), auth, (_, _) => auth);
        
        // Note: Token will be persisted to database when participant is created/updated
        return auth;
    }

    public bool TryGet(Guid sessionId, Guid participantId, out ParticipantAuth auth)
    {
        // Try cache first
        if (_cache.TryGetValue((sessionId, participantId), out auth))
        {
            return true;
        }

        // Fallback to database
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
            
            var participant = dbContext.Participants
                .AsNoTracking()
                .FirstOrDefault(p => p.SessionId == sessionId && p.Id == participantId);

            if (participant?.Token != null)
            {
                auth = new ParticipantAuth(participantId, participant.Token, participant.JoinedAt);
                _cache.TryAdd((sessionId, participantId), auth);
                return true;
            }
        }
        catch
        {
            // If database query fails, return false
        }

        auth = default!;
        return false;
    }

    public bool IsValid(Guid sessionId, Guid participantId, string token)
    {
        if (!TryGet(sessionId, participantId, out var auth))
        {
            return false;
        }

        return string.Equals(auth.Token, token, StringComparison.Ordinal);
    }
}
