using System.Collections.Concurrent;

namespace TechWayFit.Pulse.Web.Api;

public interface IFacilitatorTokenStore
{
    FacilitatorAuth Create(Guid sessionId);

    bool TryGet(Guid sessionId, out FacilitatorAuth auth);

    bool IsValid(Guid sessionId, string token);
}

public sealed record FacilitatorAuth(Guid FacilitatorId, string Token, DateTimeOffset IssuedAt);

public sealed class FacilitatorTokenStore : IFacilitatorTokenStore
{
    private readonly ConcurrentDictionary<Guid, FacilitatorAuth> _tokens = new();

    public FacilitatorAuth Create(Guid sessionId)
    {
        var auth = new FacilitatorAuth(Guid.NewGuid(), Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
        _tokens.AddOrUpdate(sessionId, auth, (_, _) => auth);
        return auth;
    }

    public bool TryGet(Guid sessionId, out FacilitatorAuth auth)
    {
        return _tokens.TryGetValue(sessionId, out auth);
    }

    public bool IsValid(Guid sessionId, string token)
    {
        if (!_tokens.TryGetValue(sessionId, out var auth))
        {
            return false;
        }

        return string.Equals(auth.Token, token, StringComparison.Ordinal);
    }
}
