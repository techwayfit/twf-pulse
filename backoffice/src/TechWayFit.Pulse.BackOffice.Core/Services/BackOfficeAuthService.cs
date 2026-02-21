using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Abstractions;
using TechWayFit.Pulse.BackOffice.Core.Entities;
using TechWayFit.Pulse.BackOffice.Core.Models.Auth;
using TechWayFit.Pulse.BackOffice.Core.Models.Audit;
using TechWayFit.Pulse.BackOffice.Core.Persistence;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Services;

public sealed class BackOfficeAuthService : IBackOfficeAuthService
{
    private readonly BackOfficeDbContext _db;
    private readonly IAuditLogService _audit;

    public BackOfficeAuthService(BackOfficeDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<BackOfficeUser?> ValidateCredentialsAsync(
        string username, string password, CancellationToken ct = default)
    {
        var record = await _db.BackOfficeUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username.Trim().ToLowerInvariant() && u.IsActive, ct);

        if (record is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, record.PasswordHash)) return null;

        return ToDomain(record);
    }

    public async Task<IReadOnlyList<BackOfficeUserSummary>> ListOperatorsAsync(CancellationToken ct = default)
    {
        return await _db.BackOfficeUsers
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new BackOfficeUserSummary(u.Id, u.Username, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt))
            .ToListAsync(ct);
    }

    public async Task<BackOfficeUser> CreateOperatorAsync(
        CreateBackOfficeUserRequest request, string createdByOperatorId, CancellationToken ct = default)
    {
        var existing = await _db.BackOfficeUsers
            .AnyAsync(u => u.Username == request.Username.Trim().ToLowerInvariant(), ct);

        if (existing)
            throw new InvalidOperationException($"Operator '{request.Username}' already exists.");

        var record = new BackOfficeUserRecord
        {
            Id           = Guid.NewGuid(),
            Username     = request.Username.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = request.Role,
            IsActive     = true,
            CreatedAt    = DateTimeOffset.UtcNow
        };

        _db.BackOfficeUsers.Add(record);
        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), createdByOperatorId, "SuperAdmin",
            "CreateOperator", "BackOfficeUser", record.Id.ToString(),
            null, null, record.Username, "New operator account created", "system",
            DateTimeOffset.UtcNow), ct);
        await _db.SaveChangesAsync(ct);

        return ToDomain(record);
    }

    public async Task UpdateRoleAsync(UpdateBackOfficeUserRoleRequest request, string operatorId, string ipAddress, CancellationToken ct = default)
    {
        var record = await _db.BackOfficeUsers.FindAsync([request.UserId], ct)
                     ?? throw new KeyNotFoundException($"BackOffice user {request.UserId} not found.");

        var oldRole = record.Role;
        record.Role = request.NewRole;

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, "SuperAdmin",
            "UpdateOperatorRole", "BackOfficeUser", record.Id.ToString(),
            "Role", oldRole, request.NewRole, request.Reason, ipAddress,
            DateTimeOffset.UtcNow), ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ToggleActiveAsync(ToggleBackOfficeUserActiveRequest request, string operatorId, string ipAddress, CancellationToken ct = default)
    {
        var record = await _db.BackOfficeUsers.FindAsync([request.UserId], ct)
                     ?? throw new KeyNotFoundException($"BackOffice user {request.UserId} not found.");

        var oldValue = record.IsActive.ToString();
        record.IsActive = request.IsActive;

        await _audit.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(), operatorId, "SuperAdmin",
            request.IsActive ? "ActivateOperator" : "DeactivateOperator",
            "BackOfficeUser", record.Id.ToString(),
            "IsActive", oldValue, request.IsActive.ToString(), request.Reason, ipAddress,
            DateTimeOffset.UtcNow), ct);
        await _db.SaveChangesAsync(ct);
    }

    private static BackOfficeUser ToDomain(BackOfficeUserRecord r) =>
        new(r.Id, r.Username, r.PasswordHash, r.Role, r.IsActive, r.CreatedAt, r.LastLoginAt);
}
