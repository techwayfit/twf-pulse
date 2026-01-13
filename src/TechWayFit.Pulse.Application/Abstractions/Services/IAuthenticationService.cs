using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IAuthenticationService
{
  /// <summary>
    /// Sends a login OTP to the specified email address.
    /// Creates a new user if one doesn't exist.
    /// </summary>
    Task<SendOtpResult> SendLoginOtpAsync(string email, string? displayName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an OTP code and returns the facilitator user if valid.
    /// </summary>
    Task<VerifyOtpResult> VerifyOtpAsync(string email, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a facilitator user by ID.
    /// </summary>
    Task<FacilitatorUser?> GetFacilitatorAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a facilitator user by email.
    /// </summary>
    Task<FacilitatorUser?> GetFacilitatorByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public sealed record SendOtpResult(bool Success, string? Message = null);

public sealed record VerifyOtpResult(bool Success, FacilitatorUser? User = null, string? ErrorMessage = null);
