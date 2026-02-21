using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Models.Sessions;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficeSessionService : IBackOfficeSessionService
{
    private readonly BackOfficeDbContext _db;
    private readonly IAuditLogService _audit;

    public BackOfficeSessionService(BackOfficeDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<SessionSearchResult> SearchAsync(SessionSearchQuery query, CancellationToken ct = default)
    {
        var q = _db.Sessions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.CodeContains))
            q = q.Where(s => s.Code.Contains(query.CodeContains));

        if (!string.IsNullOrWhiteSpace(query.TitleContains))
            q = q.Where(s => s.Title.Contains(query.TitleContains));

        if (query.Status.HasValue)
            q = q.Where(s => s.Status == (int)query.Status.Value);

        if (query.FacilitatorUserId.HasValue)
            q = q.Where(s => s.FacilitatorUserId == query.FacilitatorUserId.Value);

        var totalCount = await q.CountAsync(ct);
        var sessions = await q
            .OrderByDescending(s => s.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var participantCounts = await _db.Participants.AsNoTracking()
            .Where(p => sessionIds.Contains(p.SessionId))
            .GroupBy(p => p.SessionId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var pMap = participantCounts.ToDictionary(x => x.Key, x => x.Count);

        var activityCounts = await _db.Activities.AsNoTracking()
            .Where(a => sessionIds.Contains(a.SessionId))
            .GroupBy(a => a.SessionId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var aMap = activityCounts.ToDictionary(x => x.Key, x => x.Count);

        var ownerIds = sessions.Where(s => s.FacilitatorUserId.HasValue)
            .Select(s => s.FacilitatorUserId!.Value).Distinct().ToList();
        var ownerEmails = await _db.FacilitatorUsers.AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(ct);
        var emailMap = ownerEmails.ToDictionary(x => x.Id, x => x.Email);

        var items = sessions.Select(s => new SessionSummary(
            s.Id, s.Code, s.Title,
            (SessionStatus)s.Status,
            s.FacilitatorUserId.HasValue ? emailMap.GetValueOrDefault(s.FacilitatorUserId.Value, "-") : "-",
            s.CreatedAt, s.ExpiresAt,
            pMap.GetValueOrDefault(s.Id),
            aMap.GetValueOrDefault(s.Id))).ToList();

        return new SessionSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<SessionDetailViewModel?> GetDetailAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null) return null;

        string ownerEmail = "-";
        if (session.FacilitatorUserId.HasValue)
        {
            var owner = await _db.FacilitatorUsers.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == session.FacilitatorUserId.Value, ct);
            ownerEmail = owner?.Email ?? "-";
        }

        var activities = await _db.Activities.AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.Order)
            .Select(a => new ActivitySummary(a.Id, a.Type, a.Status.ToString(), a.Order, a.OpenedAt, a.ClosedAt))
            .ToListAsync(ct);

        var participantCount = await _db.Participants.AsNoTracking()
            .CountAsync(p => p.SessionId == sessionId, ct);

        return new SessionDetailViewModel(
            session.Id, session.Code, session.Title,
            session.Goal, session.ContextJson,
            (SessionStatus)session.Status,
            ownerEmail, session.FacilitatorUserId,
            session.CreatedAt, session.UpdatedAt, session.ExpiresAt,
            session.SettingsJson, session.JoinFormSchemaJson,
            IsAdminLocked: false,   // TODO: map once field is added
            activities, participantCount);
    }

    public async Task ForceEndAsync(ForceEndSessionRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FindAsync([request.SessionId], ct)
                      ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        var oldStatus = ((SessionStatus)session.Status).ToString();
        session.Status = (int)SessionStatus.Ended;
        session.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "ForceEndSession", "Session", request.SessionId.ToString(),
            "Status", oldStatus, SessionStatus.Ended.ToString(),
            request.Reason, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ExtendExpiryAsync(ExtendSessionExpiryRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        // Operators capped at +30 days; SuperAdmin unrestricted
        if (operatorRole == "Operator" && request.AdditionalDays > 30)
            throw new InvalidOperationException("Operators may extend expiry by at most 30 days.");

        var session = await _db.Sessions.FindAsync([request.SessionId], ct)
                      ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        var oldExpiry = session.ExpiresAt.ToString("O");
        session.ExpiresAt = session.ExpiresAt.AddDays(request.AdditionalDays);
        session.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "ExtendSessionExpiry", "Session", request.SessionId.ToString(),
            "ExpiresAt", oldExpiry, session.ExpiresAt.ToString("O"),
            request.Reason, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetLockAsync(LockSessionRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FindAsync([request.SessionId], ct)
                      ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        // TODO: map IsAdminLocked field once DB migration is run
        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            request.Lock ? "LockSession" : "UnlockSession",
            "Session", request.SessionId.ToString(),
            "IsAdminLocked", (!request.Lock).ToString(), request.Lock.ToString(),
            request.Reason, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteSessionAsync(DeleteSessionRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FindAsync([request.SessionId], ct)
                      ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        if (!string.Equals(session.Code, request.ConfirmationCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Confirmation code does not match. Delete aborted.");

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "DeleteSession", "Session", request.SessionId.ToString(),
            null, session.Code, null,
            request.Reason, ipAddress, DateTimeOffset.UtcNow), ct);

        _db.Sessions.Remove(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ForceCloseActivityAsync(Guid activityId, string reason, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FindAsync([activityId], ct)
                       ?? throw new KeyNotFoundException($"Activity {activityId} not found.");

        var oldStatus = activity.Status.ToString();
        activity.Status = (int)ActivityStatus.Closed;
        activity.ClosedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "ForceCloseActivity", "Activity", activityId.ToString(),
            "Status", oldStatus, ActivityStatus.Closed.ToString(),
            reason, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveParticipantAsync(Guid participantId, string reason, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var participant = await _db.Participants.FindAsync([participantId], ct)
                          ?? throw new KeyNotFoundException($"Participant {participantId} not found.");

        // TODO: soft-delete once IsRemoved field is added
        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "RemoveParticipant", "Participant", participantId.ToString(),
            null, participant.Name, null,
            reason, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }
}
