using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.BackOffice.Core.Models.Sessions;

public record SessionSearchQuery(
    string? CodeContains,
    string? TitleContains,
    SessionStatus? Status,
    Guid? FacilitatorUserId,
    int Page = 1,
    int PageSize = 30);

public record SessionSummary(
    Guid Id,
    string Code,
    string Title,
    SessionStatus Status,
    string OwnerEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int ParticipantCount,
    int ActivityCount);

public record SessionDetailViewModel(
    Guid Id,
    string Code,
    string Title,
    string? Goal,
    string? Context,
    SessionStatus Status,
    string OwnerEmail,
    Guid? OwnerUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset ExpiresAt,
    string SettingsJson,
    string JoinFormSchemaJson,
    bool IsAdminLocked,
    IReadOnlyList<ActivitySummary> Activities,
    int ParticipantCount);

public record ActivitySummary(
    Guid Id,
    string Type,
    string Status,
    int Order,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? ClosedAt);

public record ForceEndSessionRequest(Guid SessionId, string Reason);
public record ExtendSessionExpiryRequest(Guid SessionId, int AdditionalDays, string Reason);
public record DeleteSessionRequest(Guid SessionId, string ConfirmationCode, string Reason);
public record LockSessionRequest(Guid SessionId, bool Lock, string Reason);

public record SessionSearchResult(
    IReadOnlyList<SessionSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);
