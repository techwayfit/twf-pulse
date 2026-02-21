namespace TechWayFit.Pulse.BackOffice.Core.Models.Auth;

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public record BackOfficeUserSummary(
    Guid Id,
    string Username,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

public record CreateBackOfficeUserRequest(
    string Username,
    string Password,
    string Role);

public record UpdateBackOfficeUserRoleRequest(
    Guid UserId,
    string NewRole,
    string Reason);

public record ToggleBackOfficeUserActiveRequest(
    Guid UserId,
    bool IsActive,
    string Reason);
