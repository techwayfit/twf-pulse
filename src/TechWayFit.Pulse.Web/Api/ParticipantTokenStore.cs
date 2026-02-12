using System.Collections.Concurrent;

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
    // Key: (SessionId, ParticipantId) -> ParticipantAuth
    private readonly ConcurrentDictionary<(Guid SessionId, Guid ParticipantId), ParticipantAuth> _tokens = new();

    public ParticipantAuth Create(Guid sessionId, Guid participantId)
    {
        var auth = new ParticipantAuth(participantId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
        _tokens.AddOrUpdate((sessionId, participantId), auth, (_, _) => auth);
        return auth;
    }

    public bool TryGet(Guid sessionId, Guid participantId, out ParticipantAuth auth)
    {
        return _tokens.TryGetValue((sessionId, participantId), out auth);
    }

    public bool IsValid(Guid sessionId, Guid participantId, string token)
    {
        if (!_tokens.TryGetValue((sessionId, participantId), out var auth))
        {
            return false;
        }

        return string.Equals(auth.Token, token, StringComparison.Ordinal);
    }
}
