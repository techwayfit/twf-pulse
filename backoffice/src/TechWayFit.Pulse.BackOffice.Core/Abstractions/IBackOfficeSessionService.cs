using TechWayFit.Pulse.BackOffice.Core.Models.Sessions;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// BackOffice operations on Sessions and Activities.
/// All mutating methods write an audit record before committing.
/// </summary>
public interface IBackOfficeSessionService
{
    Task<SessionSearchResult> SearchAsync(SessionSearchQuery query, CancellationToken ct = default);
    Task<SessionDetailViewModel?> GetDetailAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Force-transition a Live session to Ended.</summary>
    Task ForceEndAsync(ForceEndSessionRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Push ExpiresAt forward by N days. Operators capped at +30 days.</summary>
    Task ExtendExpiryAsync(ExtendSessionExpiryRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Set or clear the admin lock flag on a session.</summary>
    Task SetLockAsync(LockSessionRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Hard-delete a session and all child data (SuperAdmin only). ConfirmationCode must match session Code.</summary>
    Task DeleteSessionAsync(DeleteSessionRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Force-close an Open activity within a session.</summary>
    Task ForceCloseActivityAsync(Guid activityId, string reason, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Remove a participant (soft-delete). SuperAdmin only.</summary>
    Task RemoveParticipantAsync(Guid participantId, string reason, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);
}
