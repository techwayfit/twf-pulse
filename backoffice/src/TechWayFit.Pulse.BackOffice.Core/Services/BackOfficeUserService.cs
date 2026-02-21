using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Models.Users;
using TechWayFit.Pulse.BackOffice.Core.Persistence;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficeUserService : IBackOfficeUserService
{
    private readonly BackOfficeDbContext _db;
    private readonly IAuditLogService _audit;

    public BackOfficeUserService(BackOfficeDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<UserSearchResult> SearchAsync(UserSearchQuery query, CancellationToken ct = default)
    {
        var q = _db.FacilitatorUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EmailContains))
            q = q.Where(u => u.Email.Contains(query.EmailContains));

        if (!string.IsNullOrWhiteSpace(query.NameContains))
            q = q.Where(u => u.DisplayName.Contains(query.NameContains));

        // NOTE: IsDisabled filter is not yet mapped to FacilitatorUserRecord.
        // Will apply once the field migration is added.

        var totalCount = await q.CountAsync(ct);
        var userIds = await q
            .OrderBy(u => u.Email)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var sessionCounts = await _db.Sessions
            .AsNoTracking()
            .Where(s => s.FacilitatorUserId.HasValue && userIds.Contains(s.FacilitatorUserId!.Value))
            .GroupBy(s => s.FacilitatorUserId)
            .Select(g => new { UserId = g.Key!.Value, Count = g.Count() })
            .ToListAsync(ct);

        var countMap = sessionCounts.ToDictionary(x => x.UserId, x => x.Count);

        var users = await _db.FacilitatorUsers
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        var items = users
            .OrderBy(u => u.Email)
            .Select(u => new UserSummary(
                u.Id,
                u.Email,
                u.DisplayName,
                IsDisabled: false,          // TODO: map once field added
                u.CreatedAt,
                u.LastLoginAt,
                countMap.GetValueOrDefault(u.Id)))
            .ToList();

        return new UserSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<UserDetailViewModel?> GetDetailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.FacilitatorUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

        var userData = await _db.FacilitatorUserData.AsNoTracking()
            .Where(d => d.FacilitatorUserId == userId)
            .Select(d => new UserDataEntry(d.Id, d.Key,
                d.Key.Contains("ApiKey") || d.Key.Contains("Secret"),
                d.CreatedAt, d.UpdatedAt))
            .ToListAsync(ct);

        var sessionCount = await _db.Sessions.AsNoTracking()
            .CountAsync(s => s.FacilitatorUserId == userId, ct);

        return new UserDetailViewModel(
            user.Id, user.Email, user.DisplayName,
            IsDisabled: false, DisabledReason: null, DisabledAt: null,
            user.CreatedAt, user.LastLoginAt,
            userData, sessionCount);
    }

    public async Task SetDisabledAsync(DisableUserRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var user = await _db.FacilitatorUsers.FindAsync([request.UserId], ct)
                   ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        // TODO: map IsDisabled field once DB migration is run
        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            request.Disable ? "DisableUser" : "EnableUser",
            "FacilitatorUser", request.UserId.ToString(),
            "IsDisabled", (!request.Disable).ToString(), request.Disable.ToString(),
            request.Reason, ipAddress, DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateDisplayNameAsync(UpdateUserDisplayNameRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var user = await _db.FacilitatorUsers.FindAsync([request.UserId], ct)
                   ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        var oldName = user.DisplayName;
        user.DisplayName = request.NewDisplayName.Trim();

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "UpdateDisplayName", "FacilitatorUser", request.UserId.ToString(),
            "DisplayName", oldName, request.NewDisplayName, request.Reason, ipAddress,
            DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateEmailAsync(UpdateUserEmailRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var user = await _db.FacilitatorUsers.FindAsync([request.UserId], ct)
                   ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        var oldEmail = user.Email;
        user.Email = request.NewEmail.Trim().ToLowerInvariant();

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "UpdateEmail", "FacilitatorUser", request.UserId.ToString(),
            "Email", oldEmail, request.NewEmail, request.Reason, ipAddress,
            DateTimeOffset.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteUserAsync(Guid userId, string confirmationEmail, string reason, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default)
    {
        var user = await _db.FacilitatorUsers.FindAsync([userId], ct)
                   ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (!string.Equals(user.Email, confirmationEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Confirmation email does not match. Delete aborted.");

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, operatorRole,
            "DeleteUser", "FacilitatorUser", userId.ToString(),
            null, user.Email, null, reason, ipAddress,
            DateTimeOffset.UtcNow), ct);

        _db.FacilitatorUsers.Remove(user);
        await _db.SaveChangesAsync(ct);
    }
}
