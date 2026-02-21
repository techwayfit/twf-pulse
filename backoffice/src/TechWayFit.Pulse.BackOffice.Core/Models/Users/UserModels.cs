namespace TechWayFit.Pulse.BackOffice.Core.Models.Users;

public record UserSearchQuery(
    string? EmailContains,
    string? NameContains,
    bool? IsDisabled,
    int Page = 1,
    int PageSize = 30);

public record UserSummary(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsDisabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    int SessionCount);

public record UserDetailViewModel(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsDisabled,
    string? DisabledReason,
    DateTimeOffset? DisabledAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<UserDataEntry> UserData,
    int SessionCount);

public record UserDataEntry(
    Guid Id,
    string Key,
    bool IsSensitive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record DisableUserRequest(Guid UserId, bool Disable, string Reason);
public record UpdateUserDisplayNameRequest(Guid UserId, string NewDisplayName, string Reason);
public record UpdateUserEmailRequest(Guid UserId, string NewEmail, string Reason);

public record UserSearchResult(
    IReadOnlyList<UserSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);
