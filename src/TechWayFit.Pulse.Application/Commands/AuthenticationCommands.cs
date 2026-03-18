namespace TechWayFit.Pulse.Application.Commands;

public sealed record SendLoginOtpCommand(string Email, string? DisplayName = null);

public sealed record VerifyOtpCommand(string Email, string OtpCode, string? DisplayName = null);
