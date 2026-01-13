namespace TechWayFit.Pulse.Domain.Entities;

/// <summary>
/// Represents a one-time password sent to a user's email for passwordless authentication.
/// </summary>
public sealed class LoginOtp
{
    public LoginOtp(
        Guid id,
        string email,
        string otpCode,
        DateTimeOffset createdAt,
    DateTimeOffset expiresAt,
      bool isUsed = false,
        DateTimeOffset? usedAt = null)
    {
     Id = id;
        Email = email.Trim().ToLowerInvariant();
        OtpCode = otpCode;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        IsUsed = isUsed;
  UsedAt = usedAt;
    }

    public Guid Id { get; }

  public string Email { get; }

    public string OtpCode { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool IsUsed { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsValid(DateTimeOffset now)
    {
  return !IsUsed && now < ExpiresAt;
    }

    public void MarkAsUsed(DateTimeOffset usedAt)
    {
        IsUsed = true;
        UsedAt = usedAt;
 }
}
